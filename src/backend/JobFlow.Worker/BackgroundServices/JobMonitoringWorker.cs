using JobFlow.Core.Entities;
using JobFlow.Core.Enums;
using JobFlow.Core.Interfaces;
using JobFlow.Core.Models;
using JobFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobFlow.Worker.BackgroundServices;

public class JobMonitoringWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISignalRNotificationDispatcher _dispatcher;
    private readonly ILogger<JobMonitoringWorker> _logger;

    public JobMonitoringWorker(
        IServiceScopeFactory scopeFactory,
        ISignalRNotificationDispatcher dispatcher,
        ILogger<JobMonitoringWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JobMonitoringWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DetectAndRecoverStalledJobsAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error scanning for stalled jobs in JobMonitoringWorker.");
            }

            // Check every 30 seconds
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        _logger.LogInformation("JobMonitoringWorker stopped.");
    }

    private async Task DetectAndRecoverStalledJobsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<JobFlowDbContext>();

        var now = DateTime.UtcNow;

        // Find running executions where worker lease has expired
        var stalledExecutions = await dbContext.JobExecutions
            .Include(e => e.JobDefinition)
            .Where(e => e.Status == JobStatus.Running && e.LeaseExpiresAt != null && e.LeaseExpiresAt < now)
            .ToListAsync(stoppingToken);

        foreach (var execution in stalledExecutions)
        {
            _logger.LogWarning("Stalled execution detected: {ExecutionId} (Worker: {WorkerId}, LeaseExpired: {ExpiresAt})",
                execution.Id, execution.WorkerId, execution.LeaseExpiresAt);

            // Mark as TimedOut or Failed
            execution.Status = JobStatus.TimedOut;
            execution.CompletedAt = DateTime.UtcNow;
            execution.ErrorMessage = "Worker lease expired without renewal (Worker process crash or abnormal termination detected).";

            dbContext.JobExecutionEvents.Add(new JobExecutionEvent
            {
                Id = Guid.NewGuid(),
                ExecutionId = execution.Id,
                PreviousStatus = JobStatus.Running.ToString(),
                NewStatus = JobStatus.TimedOut.ToString(),
                Reason = "Recovered from expired worker lease / crash",
                Timestamp = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid().ToString()
            });

            dbContext.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                ExecutionId = execution.Id,
                Type = NotificationType.JobTimedOut,
                Message = $"Job '{execution.JobDefinition?.Name}' timed out due to expired worker lease.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _dispatcher.DispatchJobTimedOutAsync(new JobEventPayload
            {
                JobId = execution.JobDefinitionId,
                ExecutionId = execution.Id,
                EventType = "JobTimedOut",
                Status = JobStatus.TimedOut,
                Message = "Worker lease expired without renewal."
            });
        }

        if (stalledExecutions.Count > 0)
        {
            await dbContext.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Recovered {Count} stalled executions.", stalledExecutions.Count);
        }
    }
}
