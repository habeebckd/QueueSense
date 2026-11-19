using JobFlow.Core.Entities;
using JobFlow.Core.Enums;
using JobFlow.Core.Interfaces;
using JobFlow.Core.Models;
using JobFlow.Infrastructure.Data;
using JobFlow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobFlow.Worker.BackgroundServices;

public class JobSchedulerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISignalRNotificationDispatcher _dispatcher;
    private readonly ILogger<JobSchedulerWorker> _logger;

    public JobSchedulerWorker(
        IServiceScopeFactory scopeFactory,
        ISignalRNotificationDispatcher dispatcher,
        ILogger<JobSchedulerWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JobSchedulerWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueSchedulesAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error processing due schedules in JobSchedulerWorker.");
            }

            // Check every 10 seconds for due schedules
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }

        _logger.LogInformation("JobSchedulerWorker stopped.");
    }

    private async Task ProcessDueSchedulesAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<JobFlowDbContext>();

        var now = DateTime.UtcNow;

        var dueSchedules = await dbContext.JobSchedules
            .Include(s => s.JobDefinition)
            .Where(s => s.IsEnabled && s.NextRunAt != null && s.NextRunAt <= now && s.JobDefinition!.IsEnabled)
            .ToListAsync(stoppingToken);

        foreach (var schedule in dueSchedules)
        {
            if (schedule.JobDefinition == null) continue;

            // Duplicate execution prevention: check if job is already Queued or Running
            var hasActiveExecution = await dbContext.JobExecutions
                .AnyAsync(e => e.JobDefinitionId == schedule.JobDefinitionId &&
                               (e.Status == JobStatus.Queued || e.Status == JobStatus.Running), stoppingToken);

            if (!hasActiveExecution)
            {
                var execution = new JobExecution
                {
                    Id = Guid.NewGuid(),
                    JobDefinitionId = schedule.JobDefinitionId,
                    Status = JobStatus.Queued,
                    Progress = 0,
                    AttemptNumber = 1,
                    QueuedAt = DateTime.UtcNow
                };

                dbContext.JobExecutions.Add(execution);

                dbContext.JobExecutionEvents.Add(new JobExecutionEvent
                {
                    Id = Guid.NewGuid(),
                    ExecutionId = execution.Id,
                    PreviousStatus = JobStatus.Pending.ToString(),
                    NewStatus = JobStatus.Queued.ToString(),
                    Reason = $"Triggered by schedule {schedule.Id} ({schedule.ScheduleType})",
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = Guid.NewGuid().ToString()
                });

                _logger.LogInformation("Scheduled execution {ExecutionId} created for job '{JobName}'.", execution.Id, schedule.JobDefinition.Name);

                await _dispatcher.DispatchJobQueuedAsync(new JobEventPayload
                {
                    JobId = schedule.JobDefinitionId,
                    ExecutionId = execution.Id,
                    EventType = "JobQueued",
                    Status = JobStatus.Queued,
                    Message = $"Queued via schedule ({schedule.ScheduleType})"
                });
            }
            else
            {
                _logger.LogInformation("Skipping schedule trigger for job '{JobName}' because an execution is already active.", schedule.JobDefinition.Name);
            }

            // Advance NextRunAt
            schedule.NextRunAt = JobScheduleCalculator.CalculateNextRun(schedule, DateTime.UtcNow);
        }

        if (dueSchedules.Count > 0)
        {
            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}
