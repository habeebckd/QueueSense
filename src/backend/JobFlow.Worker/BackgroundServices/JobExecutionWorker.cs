using System.Diagnostics;
using JobFlow.Core.Entities;
using JobFlow.Core.Enums;
using JobFlow.Core.Interfaces;
using JobFlow.Core.Models;
using JobFlow.Infrastructure.Data;
using JobFlow.Worker.Handlers;
using JobFlow.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobFlow.Worker.BackgroundServices;

public class JobExecutionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly JobHandlerRegistry _handlerRegistry;
    private readonly JobCancellationManager _cancellationManager;
    private readonly ISignalRNotificationDispatcher _dispatcher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JobExecutionWorker> _logger;
    private readonly string _workerId;
    private readonly SemaphoreSlim _concurrencyLimiter;

    public JobExecutionWorker(
        IServiceScopeFactory scopeFactory,
        JobHandlerRegistry handlerRegistry,
        JobCancellationManager cancellationManager,
        ISignalRNotificationDispatcher dispatcher,
        IConfiguration configuration,
        ILogger<JobExecutionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _handlerRegistry = handlerRegistry;
        _cancellationManager = cancellationManager;
        _dispatcher = dispatcher;
        _configuration = configuration;
        _logger = logger;
        _workerId = $"worker-{Environment.MachineName.ToLowerInvariant()}-{Guid.NewGuid().ToString()[..6]}";

        var maxConcurrency = int.TryParse(_configuration["Worker:MaxConcurrency"], out var mc) ? mc : 4;
        _concurrencyLimiter = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JobExecutionWorker [{WorkerId}] started with max concurrency.", _workerId);

        var pollingInterval = TimeSpan.FromSeconds(
            int.TryParse(_configuration["Worker:PollingIntervalSeconds"], out var sec) ? Math.Max(1, sec) : 2);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNextQueuedJobAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Unhandled error during worker queue polling cycle.");
            }

            await Task.Delay(pollingInterval, stoppingToken);
        }

        _logger.LogInformation("JobExecutionWorker [{WorkerId}] stopped gracefully.", _workerId);
    }

    private async Task ProcessNextQueuedJobAsync(CancellationToken stoppingToken)
    {
        // Don't dequeue if all concurrency slots are currently busy
        if (!await _concurrencyLimiter.WaitAsync(100, stoppingToken))
        {
            return;
        }

        JobExecution? executionToRun = null;
        JobDefinition? jobDef = null;

        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<JobFlowDbContext>();

            // Find next available queued execution with atomic claim
            var execution = await dbContext.JobExecutions
                .Include(e => e.JobDefinition)
                .Where(e => e.Status == JobStatus.Queued)
                .OrderBy(e => e.QueuedAt)
                .FirstOrDefaultAsync(stoppingToken);

            if (execution != null)
            {
                execution.Status = JobStatus.Running;
                execution.WorkerId = _workerId;
                execution.StartedAt = DateTime.UtcNow;
                execution.LeaseExpiresAt = DateTime.UtcNow.AddMinutes(5);

                dbContext.JobExecutionEvents.Add(new JobExecutionEvent
                {
                    Id = Guid.NewGuid(),
                    ExecutionId = execution.Id,
                    PreviousStatus = JobStatus.Queued.ToString(),
                    NewStatus = JobStatus.Running.ToString(),
                    Reason = $"Execution claimed by {_workerId}",
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = Guid.NewGuid().ToString()
                });

                await dbContext.SaveChangesAsync(stoppingToken);

                executionToRun = execution;
                jobDef = execution.JobDefinition;
            }
        }

        if (executionToRun == null || jobDef == null)
        {
            _concurrencyLimiter.Release();
            return;
        }

        // Fire and forget execution on thread pool with concurrency slot tracking
        _ = Task.Run(async () =>
        {
            try
            {
                await ExecuteClaimedJobAsync(executionToRun.Id, jobDef, stoppingToken);
            }
            finally
            {
                _concurrencyLimiter.Release();
            }
        }, stoppingToken);
    }

    private async Task ExecuteClaimedJobAsync(Guid executionId, JobDefinition jobDef, CancellationToken stoppingToken)
    {
        var linkedCts = _cancellationManager.Register(executionId, stoppingToken);
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Executing job '{JobName}' ({HandlerType}) for execution {ExecutionId} on {WorkerId}",
            jobDef.Name, jobDef.HandlerType, executionId, _workerId);

        // Notify JobStarted via SignalR
        await _dispatcher.DispatchJobStartedAsync(new JobEventPayload
        {
            JobId = jobDef.Id,
            ExecutionId = executionId,
            EventType = "JobStarted",
            Status = JobStatus.Running,
            Message = $"Execution started by {_workerId}",
            Metadata = new Dictionary<string, object?>
            {
                { "handlerType", jobDef.HandlerType },
                { "workerId", _workerId }
            }
        });

        // Check if handler is registered in allowlist
        if (!_handlerRegistry.TryGetHandler(jobDef.HandlerType, out var handler))
        {
            await HandleJobFailedAsync(executionId, jobDef.Id, $"Handler type '{jobDef.HandlerType}' is not registered in allowlist.", stopwatch.Elapsed.TotalSeconds);
            _cancellationManager.Unregister(executionId);
            return;
        }

        var executionContext = new JobExecutionContext(
            executionId,
            jobDef.Id,
            jobDef.Name,
            jobDef.Configuration,
            1,
            linkedCts.Token,
            _scopeFactory,
            _dispatcher,
            _logger);

        try
        {
            var result = await handler.ExecuteAsync(executionContext, linkedCts.Token);

            if (result.Success)
            {
                await HandleJobCompletedAsync(executionId, jobDef.Id, result.OutputJson, stopwatch.Elapsed.TotalSeconds);
            }
            else
            {
                await HandleJobFailedAsync(executionId, jobDef.Id, result.ErrorMessage ?? "Handler reported failure.", stopwatch.Elapsed.TotalSeconds);
            }
        }
        catch (OperationCanceledException)
        {
            await HandleJobCancelledAsync(executionId, jobDef.Id, stopwatch.Elapsed.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Execution {ExecutionId} threw an unhandled exception.", executionId);
            await HandleJobFailedAsync(executionId, jobDef.Id, ex.Message, stopwatch.Elapsed.TotalSeconds);
        }
        finally
        {
            _cancellationManager.Unregister(executionId);
        }
    }

    private async Task HandleJobCompletedAsync(Guid executionId, Guid jobId, string? outputJson, double durationSeconds)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<JobFlowDbContext>();

        var execution = await dbContext.JobExecutions.FindAsync(executionId);
        if (execution != null)
        {
            execution.Status = JobStatus.Completed;
            execution.CompletedAt = DateTime.UtcNow;
            execution.Duration = durationSeconds;
            execution.Progress = 100;
            execution.Result = outputJson;

            dbContext.JobExecutionEvents.Add(new JobExecutionEvent
            {
                Id = Guid.NewGuid(),
                ExecutionId = executionId,
                PreviousStatus = JobStatus.Running.ToString(),
                NewStatus = JobStatus.Completed.ToString(),
                Reason = "Execution completed successfully",
                Timestamp = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid().ToString()
            });

            await dbContext.SaveChangesAsync();
        }

        await _dispatcher.DispatchJobCompletedAsync(new JobEventPayload
        {
            JobId = jobId,
            ExecutionId = executionId,
            EventType = "JobCompleted",
            Status = JobStatus.Completed,
            Message = "Job finished successfully."
        });
    }

    private async Task HandleJobCancelledAsync(Guid executionId, Guid jobId, double durationSeconds)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<JobFlowDbContext>();

        var execution = await dbContext.JobExecutions.FindAsync(executionId);
        if (execution != null)
        {
            execution.Status = JobStatus.Cancelled;
            execution.CompletedAt = DateTime.UtcNow;
            execution.Duration = durationSeconds;
            execution.ErrorMessage = "Execution was cancelled by operator request.";

            dbContext.JobExecutionEvents.Add(new JobExecutionEvent
            {
                Id = Guid.NewGuid(),
                ExecutionId = executionId,
                PreviousStatus = JobStatus.Running.ToString(),
                NewStatus = JobStatus.Cancelled.ToString(),
                Reason = "Cancellation requested and acknowledged",
                Timestamp = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid().ToString()
            });

            await dbContext.SaveChangesAsync();
        }

        await _dispatcher.DispatchJobCancelledAsync(new JobEventPayload
        {
            JobId = jobId,
            ExecutionId = executionId,
            EventType = "JobCancelled",
            Status = JobStatus.Cancelled,
            Message = "Job execution cancelled."
        });
    }

    private async Task HandleJobFailedAsync(Guid executionId, Guid jobId, string errorMessage, double durationSeconds)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<JobFlowDbContext>();

        var execution = await dbContext.JobExecutions.FindAsync(executionId);
        if (execution != null)
        {
            execution.Status = JobStatus.Failed;
            execution.CompletedAt = DateTime.UtcNow;
            execution.Duration = durationSeconds;
            execution.ErrorMessage = errorMessage;

            dbContext.JobExecutionEvents.Add(new JobExecutionEvent
            {
                Id = Guid.NewGuid(),
                ExecutionId = executionId,
                PreviousStatus = JobStatus.Running.ToString(),
                NewStatus = JobStatus.Failed.ToString(),
                Reason = $"Execution failed: {errorMessage}",
                Timestamp = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid().ToString()
            });

            dbContext.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                ExecutionId = executionId,
                Type = NotificationType.JobFailed,
                Message = $"Job execution failed: {errorMessage}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }

        await _dispatcher.DispatchJobFailedAsync(new JobEventPayload
        {
            JobId = jobId,
            ExecutionId = executionId,
            EventType = "JobFailed",
            Status = JobStatus.Failed,
            Message = errorMessage
        });
    }
}
