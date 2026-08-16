using LanternAI.Api.Infrastructure;
using LanternAI.Api.Services.Llm;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace LanternAI.Api.Tests;

public class OllamaHealthCheckTests
{
    private static IOptions<OllamaOptions> CreateOptions(string model, string baseUrl = "http://localhost:11434") =>
        Options.Create(new OllamaOptions { BaseUrl = baseUrl, Model = model });

    private static IHttpClientFactory CreateFactory(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler);
        var factory = new FakeHttpClientFactory(client);
        return factory;
    }

    private sealed class FakeHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class FakeHttpMessageHandler(string responseJson, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }

    private static HealthCheckContext CreateContext() => new()
    {
        Registration = new HealthCheckRegistration("ollama", _ => null!, null, null),
    };

    [Fact]
    public async Task CheckHealthAsync_ModelPresentInTags_ReturnsHealthy()
    {
        var json = """{"models":[{"name":"qwen2.5-coder:1.5b"}]}""";
        var factory = CreateFactory(new FakeHttpMessageHandler(json));
        var check = new OllamaHealthCheck(factory, CreateOptions("qwen2.5-coder:1.5b"));

        var result = await check.CheckHealthAsync(CreateContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_ModelMatchesByBaseName_ReturnsHealthy()
    {
        var json = """{"models":[{"name":"qwen2.5-coder:latest"}]}""";
        var factory = CreateFactory(new FakeHttpMessageHandler(json));
        var check = new OllamaHealthCheck(factory, CreateOptions("qwen2.5-coder"));

        var result = await check.CheckHealthAsync(CreateContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_ModelNotInLocalTags_ReturnsDegraded()
    {
        // Cloud/remote models (e.g. qwen3-coder:480b-cloud) are not listed in local api/tags.
        var json = """{"models":[{"name":"qwen2.5-coder:1.5b"}]}""";
        var factory = CreateFactory(new FakeHttpMessageHandler(json));
        var check = new OllamaHealthCheck(factory, CreateOptions("qwen3-coder:480b-cloud"));

        var result = await check.CheckHealthAsync(CreateContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("qwen3-coder:480b-cloud", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_EmptyModelList_ReturnsDegraded()
    {
        var json = """{"models":[]}""";
        var factory = CreateFactory(new FakeHttpMessageHandler(json));
        var check = new OllamaHealthCheck(factory, CreateOptions("qwen2.5-coder:1.5b"));

        var result = await check.CheckHealthAsync(CreateContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_OllamaServerUnreachable_ReturnsUnhealthy()
    {
        var handler = new ThrowingHttpMessageHandler(new HttpRequestException("Connection refused"));
        var factory = CreateFactory(handler);
        var check = new OllamaHealthCheck(factory, CreateOptions("qwen2.5-coder:1.5b"));

        var result = await check.CheckHealthAsync(CreateContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_OllamaReturnsNonSuccess_ReturnsUnhealthy()
    {
        var factory = CreateFactory(new FakeHttpMessageHandler("{}", System.Net.HttpStatusCode.InternalServerError));
        var check = new OllamaHealthCheck(factory, CreateOptions("qwen2.5-coder:1.5b"));

        var result = await check.CheckHealthAsync(CreateContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_CloudModelPresentInTags_ReturnsHealthy()
    {
        // Ollama Cloud api/tags lists cloud models (e.g. qwen3.5:397b).
        var json = """{"models":[{"name":"qwen3.5:397b"}]}""";
        var factory = CreateFactory(new FakeHttpMessageHandler(json));
        var check = new OllamaHealthCheck(factory, CreateOptions("qwen3.5:397b", "https://ollama.com"));

        var result = await check.CheckHealthAsync(CreateContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_CloudReturnsUnauthorized_ReturnsUnhealthy()
    {
        var factory = CreateFactory(new FakeHttpMessageHandler("Unauthorized", System.Net.HttpStatusCode.Unauthorized));
        var check = new OllamaHealthCheck(factory, CreateOptions("qwen3.5:397b", "https://ollama.com"));

        var result = await check.CheckHealthAsync(CreateContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("401", result.Description);
    }

    private sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(exception);
    }
}
