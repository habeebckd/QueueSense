using System.Text.Json;
using JobFlow.Core.Entities;
using JobFlow.Core.Interfaces;
using JobFlow.Core.Models;
using JobFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobFlow.Infrastructure.Services;

public class AiAnalysisService
{
    private readonly JobFlowDbContext _dbContext;
    private readonly ITelemetrySanitizer _sanitizer;
    private readonly IEnumerable<IAiAnalysisProvider> _providers;
    private readonly IConfiguration _configuration;
    private readonly ISignalRNotificationDispatcher _dispatcher;
    private readonly ILogger<AiAnalysisService> _logger;

    public AiAnalysisService(
        JobFlowDbContext dbContext,
        ITelemetrySanitizer sanitizer,
        IEnumerable<IAiAnalysisProvider> providers,
        IConfiguration configuration,
        ISignalRNotificationDispatcher dispatcher,
        ILogger<AiAnalysisService> logger)
    {
        _dbContext = dbContext;
        _sanitizer = sanitizer;
        _providers = providers;
        _configuration = configuration;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task<AIAnalysisResult?> AnalyzeExecutionFailureAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.JobExecutions
            .Include(e => e.JobDefinition)
            .Include(e => e.Logs)
            .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);

        if (execution == null)
        {
            _logger.LogWarning("Execution {ExecutionId} not found for AI analysis.", executionId);
            return null;
        }

        // Notify that AI analysis has started
        await _dispatcher.DispatchAIAnalysisStartedAsync(new JobEventPayload
        {
            JobId = execution.JobDefinitionId,
            ExecutionId = execution.Id,
            EventType = "AIAnalysisStarted",
            Status = execution.Status,
            Message = "AI failure analysis initiated."
        });

        // Prepare context with sanitized logs and sanitized error message
        var recentLogs = execution.Logs
            .OrderByDescending(l => l.SequenceNumber)
            .Take(10)
            .OrderBy(l => l.SequenceNumber)
            .Select(l => _sanitizer.Sanitize(l.Message))
            .ToList();

        var sanitizedError = _sanitizer.Sanitize(execution.ErrorMessage ?? "Unknown failure");

        var analysisContext = new AiAnalysisContext
        {
            ExecutionId = execution.Id,
            JobName = execution.JobDefinition?.Name ?? "Unknown Job",
            HandlerType = execution.JobDefinition?.HandlerType ?? "Unknown Handler",
            ErrorMessage = sanitizedError,
            RecentLogs = recentLogs,
            DurationSeconds = execution.Duration ?? 0,
            AttemptNumber = execution.AttemptNumber,
            JobConfiguration = execution.JobDefinition?.Configuration
        };

        // Select configured provider
        var selectedProviderName = _configuration["AI:Provider"] ?? "Mock";
        var provider = _providers.FirstOrDefault(p => string.Equals(p.ProviderName, selectedProviderName, StringComparison.OrdinalIgnoreCase))
                       ?? _providers.First(p => p.ProviderName == "Mock");

        _logger.LogInformation("Analyzing execution failure {ExecutionId} using provider {ProviderName}", executionId, provider.ProviderName);

        AiAnalysisOutput output;
        try
        {
            output = await provider.AnalyzeFailureAsync(analysisContext, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI Provider {ProviderName} threw an exception. Falling back to default diagnosis.", provider.ProviderName);
            output = new AiAnalysisOutput
            {
                Summary = $"Automated analysis encountered an internal error: {_sanitizer.Sanitize(ex.Message)}",
                ProbableCauses = new List<string> { "Diagnostics engine timeout or transient failure" },
                SuggestedSolutions = new List<string> { "Inspect raw execution logs manually", "Retry execution if transient" },
                Severity = "Medium",
                Confidence = "Low",
                RetryRecommended = true
            };
        }

        // Check if an AIAnalysisResult already exists for this execution
        var existingResult = await _dbContext.AIAnalysisResults.FirstOrDefaultAsync(a => a.ExecutionId == executionId, cancellationToken);
        var resultRecord = existingResult ?? new AIAnalysisResult
        {
            Id = Guid.NewGuid(),
            ExecutionId = executionId
        };

        resultRecord.Summary = output.Summary;
        resultRecord.ProbableCauses = JsonSerializer.Serialize(output.ProbableCauses);
        resultRecord.SuggestedSolutions = JsonSerializer.Serialize(output.SuggestedSolutions);
        resultRecord.Severity = output.Severity;
        resultRecord.Confidence = output.Confidence;
        resultRecord.RetryRecommended = output.RetryRecommended;
        resultRecord.CreatedAt = DateTime.UtcNow;

        if (existingResult == null)
        {
            _dbContext.AIAnalysisResults.Add(resultRecord);
        }

        // Create notification
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            ExecutionId = execution.Id,
            Type = Core.Enums.NotificationType.AiAnalysisReady,
            Message = $"AI Failure Analysis ready for job '{execution.JobDefinition?.Name}': {output.Summary}",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Notifications.Add(notification);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Broadcast completion via SignalR
        await _dispatcher.DispatchAIAnalysisCompletedAsync(new AiAnalysisEventPayload
        {
            ExecutionId = execution.Id,
            EventType = "AIAnalysisCompleted",
            Timestamp = DateTime.UtcNow,
            Summary = output.Summary,
            Severity = output.Severity,
            Confidence = output.Confidence,
            RetryRecommended = output.RetryRecommended
        });

        await _dispatcher.DispatchNotificationAsync(new NotificationPayload
        {
            Id = notification.Id,
            ExecutionId = execution.Id,
            Type = notification.Type.ToString(),
            Message = notification.Message,
            CreatedAt = notification.CreatedAt
        });

        return resultRecord;
    }
}
