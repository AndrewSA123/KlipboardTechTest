using FaultTriage.Core;
using FaultTriage.Infrastructure.Configuration;
using FaultTriage.Infrastructure.Exceptions;
using FaultTriage.Infrastructure.Models.Groq;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FaultTriage.Infrastructure.FaultAnalysers;

public class GroqFaultAnalyser : IFaultAnalyser
{
    private readonly HttpClient _httpClient;
    private readonly GroqOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private const string SystemPrompt = """
        You are a triage assistant for a vehicle garage's service desk. A customer has
        described a fault with their vehicle in their own words. Your job is to help a
        service adviser quickly understand the issue.

        Respond with ONLY a JSON object, no other text, matching exactly this schema:
        {
          "summary": string,
          "affectedSystems": string[],
          "severity": "Routine" | "NeedsAttention" | "SafetyCritical",
          "clarifyingQuestions": string[],
          "suggestedNextSteps": string[]
        }

        Use "SafetyCritical" for anything involving brakes, steering, tyres, or sudden loss
        of control. Be concise and practical - this is read by an experienced service adviser,
        not a customer.
        """;

    public GroqFaultAnalyser(HttpClient httpClient, IOptions<GroqOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<FaultAnalysis> AnalyzeAsync(string faultDescription, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(faultDescription))
        {
            throw new ArgumentException("Fault description cannot be empty.", nameof(faultDescription));
        }

        var requestBody = new
        {
            model = _options.Model,
            response_format = new { type = "json_object" },
            temperature = 0.2,
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = faultDescription }
            }
        };

        using var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new FaultAnalyserException($"Groq API returned {(int)response.StatusCode}: {errorBody}");
        }

        var completion = await response.Content
            .ReadFromJsonAsync<GroqChatCompletionResponse>(JsonOptions, cancellationToken)
            ?? throw new FaultAnalyserException("Groq API returned an empty response.");

        var content = completion.Choices.FirstOrDefault()?.Message.Content
            ?? throw new FaultAnalyserException("Groq API response contained no message content.");

        try
        {
            return JsonSerializer.Deserialize<FaultAnalysis>(content, JsonOptions)
                ?? throw new FaultAnalyserException("Failed to parse fault analysis from model response.");
        }
        catch (JsonException ex)
        {
            throw new FaultAnalyserException("Model response was not valid JSON matching the expected schema.", ex);
        }
    }
}