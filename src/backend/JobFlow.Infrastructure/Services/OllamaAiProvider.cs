using System.Text;
using System.Text.Json;
using JobFlow.Core.Interfaces;
using JobFlow.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobFlow.Infrastructure.Services;

public class OllamaAiProvider : IAiAnalysisProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OllamaAiProvider> _logger;
    private readonly MockAiProvider _fallbackProvider;

    public OllamaAiProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OllamaAiProvider> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _fallbackProvider = new MockAiProvider();
    }

    public string ProviderName => "Ollama";

    public async Task<AiAnalysisOutput> AnalyzeFailureAsync(AiAnalysisContext context, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["AI:Ollama:BaseUrl"] ?? "http://localhost:11434";
        var model = _configuration["AI:Ollama:Model"] ?? "llama3";

        var prompt = $"Analyze this background job failure and return valid JSON with keys 'summary' (string), 'probableCauses' (string[]), 'suggestedSolutions' (string[]), 'severity' (string 'Low'|'Medium'|'High'|'Critical'), 'confidence' (string 'Low'|'Medium'|'High'), 'retryRecommended' (boolean).\n\n" +
                     $"Job: {context.JobName}\n" +
                     $"Handler: {context.HandlerType}\n" +
                     $"Attempt: {context.AttemptNumber}\n" +
                     $"Error: {context.ErrorMessage}\n" +
                     $"Logs: {string.Join(" | ", context.RecentLogs.TakeLast(5))}";

        var requestBody = new
        {
            model = model,
            prompt = prompt,
            format = "json",
            stream = false
        };

        try
        {
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{baseUrl.TrimEnd('/')}/api/generate", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(responseString);
                if (doc.RootElement.TryGetProperty("response", out var respElement))
                {
                    var innerJson = respElement.GetString();
                    if (!string.IsNullOrWhiteSpace(innerJson))
                    {
                        var parsed = JsonSerializer.Deserialize<AiAnalysisOutput>(innerJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (parsed != null && !string.IsNullOrWhiteSpace(parsed.Summary))
                        {
                            return parsed;
                        }
                    }
                }
            }
            else
            {
                _logger.LogWarning("Ollama returned status code {StatusCode}. Falling back to Mock.", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama connection failed. Falling back to MockAiProvider.");
        }

        return await _fallbackProvider.AnalyzeFailureAsync(context, cancellationToken);
    }
}
