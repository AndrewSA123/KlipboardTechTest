using System.Net;
using System.Text;
using FaultTriage.Core;
using FaultTriage.Infrastructure.Configuration;
using FaultTriage.Infrastructure.Exceptions;
using FaultTriage.Infrastructure.FaultAnalysers;
using Microsoft.Extensions.Options;
using Xunit;

namespace FaultTriage.Tests.FaultAnalysers;

public class GroqFaultAnalyserTests
{
    private static GroqFaultAnalyser CreateAnalyser(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://fake-groq-endpoint.test/")
        };

        var options = Options.Create(new GroqOptions
        {
            ApiKey = "fake-key",
            Model = "fake-model",
            BaseUrl = "https://fake-groq-endpoint.test/"
        });

        return new GroqFaultAnalyser(httpClient, options);
    }

    private static HttpResponseMessage BuildGroqSuccessResponse(string modelJsonContent)
    {
        // Mirrors Groq's actual chat completion response shape
        var wrapper = $$"""
            {
              "choices": [
                {
                  "message": {
                    "content": {{System.Text.Json.JsonSerializer.Serialize(modelJsonContent)}}
                  }
                }
              ]
            }
            """;

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(wrapper, Encoding.UTF8, "application/json")
        };
    }

    [Fact]
    public async Task AnalyzeAsync_WithValidModelJson_ReturnsPopulatedFaultAnalysis()
    {
        var modelJson = """
            {
              "summary": "Brakes are grinding and the engine light is on.",
              "affectedSystems": ["Brakes", "Engine"],
              "severity": "SafetyCritical",
              "clarifyingQuestions": ["When did the noise start?"],
              "suggestedNextSteps": ["Inspect brake pads", "Read engine fault codes"]
            }
            """;

        var handler = new FakeHttpMessageHandler(_ => BuildGroqSuccessResponse(modelJson));
        var analyser = CreateAnalyser(handler);

        var result = await analyser.AnalyzeAsync("grinding noise when braking, engine light on");

        Assert.Equal(Severity.SafetyCritical, result.Severity);
        Assert.Contains("Brakes", result.AffectedSystems);
        Assert.Contains("Engine", result.AffectedSystems);
        Assert.NotEmpty(result.ClarifyingQuestions);
        Assert.NotEmpty(result.SuggestedNextSteps);
    }

    [Fact]
    public async Task AnalyzeAsync_WhenGroqReturnsNonSuccessStatus_ThrowsFaultAnalyserExceptionWithStatusCode()
    {
        var errorBody = """{"error":{"message":"Invalid API Key","type":"invalid_request_error","code":"invalid_api_key"}}""";

        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent(errorBody, Encoding.UTF8, "application/json")
        });

        var analyser = CreateAnalyser(handler);

        var ex = await Assert.ThrowsAsync<FaultAnalyserException>(
            () => analyser.AnalyzeAsync("grinding noise when braking"));

        Assert.Contains("401", ex.Message);
    }

    [Fact]
    public async Task AnalyzeAsync_WhenModelReturnsMalformedJson_ThrowsFaultAnalyserException()
    {
        var handler = new FakeHttpMessageHandler(_ => BuildGroqSuccessResponse("this is not valid json at all"));
        var analyser = CreateAnalyser(handler);

        await Assert.ThrowsAsync<FaultAnalyserException>(
            () => analyser.AnalyzeAsync("grinding noise when braking"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AnalyzeAsync_WithEmptyOrWhitespaceDescription_ThrowsWithoutCallingGroq(string description)
    {
        var handler = new FakeHttpMessageHandler(_ =>
            throw new InvalidOperationException("HTTP call should not have been made for empty input."));

        var analyser = CreateAnalyser(handler);

        await Assert.ThrowsAsync<ArgumentException>(
            () => analyser.AnalyzeAsync(description));
    }
}