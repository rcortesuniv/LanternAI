using LanternAI.Api.Endpoints;
using LanternAI.Api.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using LanternAI.Api.Services.Catalog;
using LanternAI.Api.Services.Execution;
using LanternAI.Api.Services.Llm;
using LanternAI.Api.Services.QueryPlanning;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration -----------------------------------------------------
builder.Services.AddOptions<OllamaOptions>()
    .Bind(builder.Configuration.GetSection(OllamaOptions.SectionName))
    .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https", "Ollama:BaseUrl must be an absolute HTTP(S) URL.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Model), "Ollama:Model is required.")
    .Validate(options => options.TimeoutSeconds is >= 1 and <= 300, "Ollama:TimeoutSeconds must be between 1 and 300.")
    .Validate(options => options.HealthTimeoutSeconds is >= 1 and <= 60, "Ollama:HealthTimeoutSeconds must be between 1 and 60.")
    .ValidateOnStart();
builder.Services.AddOptions<SecurityOptions>()
    .Bind(builder.Configuration.GetSection(SecurityOptions.SectionName))
    .Validate(options => !options.Enabled || (Uri.TryCreate(options.Authority, UriKind.Absolute, out _) && !string.IsNullOrWhiteSpace(options.ClientId)), "Authentication:Authority and ClientId are required when authentication is enabled.")
    .ValidateOnStart();

var securityOptions = builder.Configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() ?? new SecurityOptions();
if (securityOptions.Enabled)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = securityOptions.Authority;
            options.Audience = securityOptions.ClientId;
            options.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true };
        });
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("Lantern.User", policy => policy.RequireAuthenticatedUser().RequireRole(securityOptions.RequiredRoles));
        options.AddPolicy("Lantern.Admin", policy => policy.RequireAuthenticatedUser().RequireRole("Lantern.Admin"));
    });
}

// --- Core services -------------------------------------------------------
builder.Services.AddSingleton<IEventTableCatalog, InMemoryEventTableCatalog>();
builder.Services.AddSingleton<IQueryExecutor, SimulatedQueryExecutor>();
builder.Services.AddSingleton<IQueryPlanService, QueryPlanService>();
builder.Services.AddSingleton<QueryCostEstimator>();
builder.Services.AddSingleton<IDataSourceCapabilitiesProvider, SimulatedDataSourceCapabilitiesProvider>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IAuditStore, InMemoryAuditStore>();
builder.Services.AddHealthChecks().AddCheck<OllamaHealthCheck>("ollama", tags: ["ready"]);

// Ollama is the Phase 1 LLM provider. To switch to Gemini once implemented,
// replace this registration with GeminiLlmProvider — ILlmProvider is the
// only thing QueryPlanService depends on.
builder.Services.AddHttpClient<ILlmProvider, OllamaLlmProvider>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    if (!string.IsNullOrWhiteSpace(options.ApiKey))
    {
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
    }
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

// Named HttpClient for the health check — shares BaseAddress and auth header
// with the typed LLM client but uses a shorter timeout (HealthTimeoutSeconds).
// This avoids duplicating config logic in OllamaHealthCheck itself.
builder.Services.AddHttpClient("ollama-health", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    if (!string.IsNullOrWhiteSpace(options.ApiKey))
    {
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
    }
    client.Timeout = TimeSpan.FromSeconds(options.HealthTimeoutSeconds);
});

// --- Error handling --------------------------------------------------------
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

// --- CORS: locked to the configured frontend origin(s) only ----------------
// AllowedOrigins is an exact-match allow-list (e.g. the local dev server).
// AllowedOriginSuffixes additionally allows any HTTPS origin whose host ends
// with one of these suffixes — used for GitHub Codespaces' per-session
// forwarded-port hostnames (https://<codespace>-5173.app.github.dev), which
// can't be known in advance as an exact origin. Each forwarded Codespaces
// port is only reachable by the authenticated owner of that Codespace, so
// this is a reasonable convenience for the demo; revisit alongside real
// auth before production (see docs/SECURITY.md).
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
var allowedOriginSuffixes = builder.Configuration.GetSection("Cors:AllowedOriginSuffixes").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .SetIsOriginAllowed(origin =>
            allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase) ||
            (Uri.TryCreate(origin, UriKind.Absolute, out var originUri)
                && originUri.Scheme == Uri.UriSchemeHttps
                && allowedOriginSuffixes.Any(suffix => originUri.Host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))))
        .AllowAnyHeader()
        .WithMethods("GET", "POST"));
});

// --- Rate limiting: guards the one endpoint that triggers LLM inference ----
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(RateLimiting.QueryPolicy, opt =>
    {
        opt.PermitLimit = 20;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 2;
    });
    options.OnRejected = async (context, _) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await ValueTask.CompletedTask;
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var supplied)
        && Guid.TryParse(supplied.FirstOrDefault(), out var parsed)
        ? parsed.ToString()
        : Guid.NewGuid().ToString();
    context.TraceIdentifier = correlationId;
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

app.Use(async (context, next) =>
{
    if (context.Request.ContentLength is > 16 * 1024)
    {
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        await context.Response.WriteAsJsonAsync(new { title = "Request too large", status = 413, detail = "Request payloads must be 16 KB or smaller." });
        return;
    }
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
if (securityOptions.Enabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
    app.Use(async (context, next) =>
    {
        if (context.User.GetTenantContext(securityOptions.TenantClaim) is null)
        {
            await Results.Problem("A tenant and subject claim are required.", statusCode: StatusCodes.Status403Forbidden).ExecuteAsync(context);
            return;
        }
        await next();
    });
}

// NOTE (auth extension point): this is a no-auth demo (single-user, local).
// Before any shared/production deployment, add Entra ID authentication here,
// e.g.:
//   builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//       .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
//   app.UseAuthentication();
//   app.UseAuthorization();
//   ...and add .RequireAuthorization() to the endpoint groups below.
// See docs/SECURITY.md for the full checklist.

app.UseCors("Frontend");
app.UseRateLimiter();

app.MapTablesEndpoints(securityOptions.Enabled);
app.MapQueryEndpoints(securityOptions.Enabled);
app.MapCapabilitiesEndpoints();
app.MapAuditEndpoints(securityOptions.Enabled);
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false,
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.Run();

// Exposed for WebApplicationFactory-based integration tests, if added later.
public partial class Program;
