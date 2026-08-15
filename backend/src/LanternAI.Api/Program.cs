using LanternAI.Api.Endpoints;
using LanternAI.Api.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using LanternAI.Api.Services.Catalog;
using LanternAI.Api.Services.Execution;
using LanternAI.Api.Services.Llm;
using LanternAI.Api.Services.QueryPlanning;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration -----------------------------------------------------
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection(OllamaOptions.SectionName));

// --- Core services -------------------------------------------------------
builder.Services.AddSingleton<IEventTableCatalog, InMemoryEventTableCatalog>();
builder.Services.AddSingleton<IQueryExecutor, SimulatedQueryExecutor>();
builder.Services.AddSingleton<IQueryPlanService, QueryPlanService>();

// Ollama is the Phase 1 LLM provider. To switch to Gemini once implemented,
// replace this registration with GeminiLlmProvider — ILlmProvider is the
// only thing QueryPlanService depends on.
builder.Services.AddHttpClient<ILlmProvider, OllamaLlmProvider>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

// --- Error handling --------------------------------------------------------
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

// --- CORS: locked to the configured frontend origin(s) only ----------------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(allowedOrigins)
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
        opt.QueueLimit = 0;
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

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

app.MapTablesEndpoints();
app.MapQueryEndpoints();

app.Run();

// Exposed for WebApplicationFactory-based integration tests, if added later.
public partial class Program;
