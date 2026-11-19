using JobFlow.Core.Entities;
using JobFlow.Core.Enums;
using JobFlow.Core.Interfaces;
using JobFlow.Core.Models;
using JobFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JobFlow.Worker.Handlers;

public class JobExecutionContext : IJobExecutionContext
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISignalRNotificationDispatcher _dispatcher;
    private readonly ILogger _logger;
    private long _logSequenceNumber;

    public Guid ExecutionId { get; }
    public Guid JobId { get; }
    public string JobName { get; }
    public string? Configuration { get; }
    public int AttemptNumber { get; }
    public CancellationToken CancellationToken { get; }

    public JobExecutionContext(
        Guid executionId,
        Guid jobId,
        string jobName,
        string? configuration,
        int attemptNumber,
        CancellationToken cancellationToken,
        IServiceScopeFactory scopeFactory,
        ISignalRNotificationDispatcher dispatcher,
        ILogger logger)
    {
        ExecutionId = executionId;
        JobId = jobId;
        JobName = jobName;
        Configuration = configuration;
        AttemptNumber = attemptNumber;
        CancellationToken = cancellationToken;
        _scopeFactory = scopeFactory;
        _dispatcher = dispatcher;
        _logger = logger;
        _logSequenceNumber = 0;
    }

    public async Task ReportProgressAsync(int percentage, string? message = null)
    {
        percentage = Math.Clamp(percentage, 0, 100);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<JobFlowDbContext>();

            var execution = await dbContext.JobExecutions.FindAsync(ExecutionId);
            if (execution != null)
            {
                execution.Progress = percentage;
                // Extend worker lease on progress heartbeat
                execution.LeaseExpiresAt = DateTime.UtcNow.AddMinutes(5);
                await dbContext.SaveChangesAsync();
            }

            await _dispatcher.DispatchJobProgressUpdatedAsync(new JobProgressPayload
            {
                JobId = JobId,
                ExecutionId = ExecutionId,
                ProgressPercentage = percentage,
                Status = JobStatus.Running,
                Message = message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist or dispatch progress update for execution {ExecutionId}", ExecutionId);
        }
    }

    public async Task LogAsync(LogLevelType level, string message)
    {
        var seq = Interlocked.Increment(ref _logSequenceNumber);
        var timestamp = DateTime.UtcNow;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<JobFlowDbContext>();

            var logEntry = new JobLog
            {
                Id = Guid.NewGuid(),
                ExecutionId = ExecutionId,
                Level = level,
                Message = message,
                Timestamp = timestamp,
                SequenceNumber = seq
            };

            dbContext.JobLogs.Add(logEntry);
            await dbContext.SaveChangesAsync();

            await _dispatcher.DispatchJobLogAddedAsync(new JobLogPayload
            {
                ExecutionId = ExecutionId,
                Level = level.ToString(),
                Message = message,
                Timestamp = timestamp,
                SequenceNumber = seq
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist or stream log for execution {ExecutionId}", ExecutionId);
        }
    }
}
