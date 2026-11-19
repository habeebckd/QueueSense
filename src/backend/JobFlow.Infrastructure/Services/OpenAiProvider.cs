using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JobFlow.Core.Interfaces;
using JobFlow.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobFlow.Infrastructure.Services;

public class OpenAiProvider : IAiAnalysisProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiProvider> _logger;
    private readonly MockAiProvider _fallbackProvider;

    public OpenAiProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAiProvider> _logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        this._logger = _logger;
        _fallbackProvider = new MockAiProvider();
    }

    public string ProviderName => "OpenAI";

    public async Task<AiAnalysisOutput> AnalyzeFailureAsync(AiAnalysisContext context, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["AI:OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("OpenAI API key not configured. Falling back to MockAiProvider.");
            return await _fallbackProvider.AnalyzeFailureAsync(context, cancellationToken);
        }

        var baseUrl = _configuration["AI:OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";
        var model = _configuration["AI:OpenAI:Model"] ?? "gpt-4o";

        var prompt = BuildPrompt(context);

        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = "You are an expert site reliability and backend software engineer analyzing background job execution failures. You must respond with a strictly valid JSON object conforming to this schema: {\"summary\": string, \"probableCauses\": string[], \"suggestedSolutions\": string[], \"severity\": \"Low\"|\"Medium\"|\"High\"|\"Critical\", \"confidence\": \"Low\"|\"Medium\"|\"High\", \"retryRecommended\": boolean}." },
                new { role = "user", content = prompt }
            },
            response_format = new { type = "json_object" },
            temperature = 0.2
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("OpenAI API call returned status {StatusCode}: {ErrorBody}. Falling back to MockAiProvider.", response.StatusCode, errorBody);
                return await _fallbackProvider.AnalyzeFailureAsync(context, cancellationToken);
            }

            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseString);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (!string.IsNullOrWhiteSpace(content))
            {
                var parsed = JsonSerializer.Deserialize<AiAnalysisOutput>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (parsed != null && !string.IsNullOrWhiteSpace(parsed.Summary))
                {
                    return parsed;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception communicating with OpenAI API. Falling back to MockAiProvider.");
        }

        return await _fallbackProvider.AnalyzeFailureAsync(context, cancellationToken);
    }

    private static string BuildPrompt(AiAnalysisContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Job Name: {context.JobName}");
        sb.AppendLine($"Handler Type: {context.HandlerType}");
        sb.AppendLine($"Attempt Number: {context.AttemptNumber}");
        sb.AppendLine($"Duration (Seconds): {context.DurationSeconds}");
        sb.AppendLine($"Error Message: {context.ErrorMessage}");
        if (!string.IsNullOrWhiteSpace(context.StackTrace))
        {
            sb.AppendLine($"Stack Trace:\n{context.StackTrace}");
        }
        if (context.RecentLogs.Count > 0)
        {
            sb.AppendLine("Recent Execution Logs:");
            foreach (var log in context.RecentLogs.TakeLast(10))
            {
                sb.AppendLine($"- {log}");
            }
        }
        return sb.ToString();
    }
}
