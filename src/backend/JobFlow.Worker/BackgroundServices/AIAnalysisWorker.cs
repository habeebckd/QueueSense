using JobFlow.Core.Enums;
using JobFlow.Infrastructure.Data;
using JobFlow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobFlow.Worker.BackgroundServices;

public class AIAnalysisWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AIAnalysisWorker> _logger;

    public AIAnalysisWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<AIAnalysisWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AIAnalysisWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingFailuresAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error processing pending AI failure analyses.");
            }

            // Check every 5 seconds for new failures to analyze
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.LogInformation("AIAnalysisWorker stopped.");
    }

    private async Task ProcessPendingFailuresAsync(CancellationToken stoppingToken)
    {
        List<Guid> failedExecutionIds;

        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<JobFlowDbContext>();

            // Find executions that failed or timed out and do not yet have an AIAnalysisResult
            failedExecutionIds = await dbContext.JobExecutions
                .Where(e => (e.Status == JobStatus.Failed || e.Status == JobStatus.TimedOut) && e.AIAnalysisResult == null)
                .OrderBy(e => e.CompletedAt)
                .Take(5)
                .Select(e => e.Id)
                .ToListAsync(stoppingToken);
        }

        foreach (var executionId in failedExecutionIds)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var aiService = scope.ServiceProvider.GetRequiredService<AiAnalysisService>();

                _logger.LogInformation("Triggering automated AI failure analysis for execution {ExecutionId}", executionId);
                await aiService.AnalyzeExecutionFailureAsync(executionId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete AI analysis for execution {ExecutionId}", executionId);
            }
        }
    }
}
