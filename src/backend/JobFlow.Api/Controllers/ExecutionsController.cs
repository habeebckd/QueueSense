using JobFlow.Core.Entities;
using JobFlow.Core.Enums;
using JobFlow.Core.Interfaces;
using JobFlow.Core.Models;
using JobFlow.Infrastructure.Data;
using JobFlow.Worker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Api.Controllers;

[Authorize]
[ApiController]
public class ExecutionsController : ControllerBase
{
    private readonly JobFlowDbContext _dbContext;
    private readonly ISignalRNotificationDispatcher _dispatcher;
    private readonly JobCancellationManager _cancellationManager;

    public ExecutionsController(
        JobFlowDbContext dbContext,
        ISignalRNotificationDispatcher dispatcher,
        JobCancellationManager cancellationManager)
    {
        _dbContext = dbContext;
        _dispatcher = dispatcher;
        _cancellationManager = cancellationManager;
    }

    [HttpPost("api/jobs/{jobId:guid}/run")]
    public async Task<IActionResult> RunJobNow(Guid jobId, [FromBody] RunJobRequest? request)
    {
        var job = await _dbContext.JobDefinitions.FindAsync(jobId);
        if (job == null)
        {
            return NotFound(new { success = false, message = "Job definition not found." });
        }

        if (!job.IsEnabled)
        {
            return BadRequest(new { success = false, message = "Cannot run a disabled job." });
        }

        var execution = new JobExecution
        {
            Id = Guid.NewGuid(),
            JobDefinitionId = jobId,
            Status = JobStatus.Queued,
            Progress = 0,
            AttemptNumber = 1,
            QueuedAt = DateTime.UtcNow
        };

        _dbContext.JobExecutions.Add(execution);

        _dbContext.JobExecutionEvents.Add(new JobExecutionEvent
        {
            Id = Guid.NewGuid(),
            ExecutionId = execution.Id,
            PreviousStatus = JobStatus.Pending.ToString(),
            NewStatus = JobStatus.Queued.ToString(),
            Reason = "Immediate run triggered by user ('Run Now')",
            Timestamp = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid().ToString()
        });

        await _dbContext.SaveChangesAsync();

        await _dispatcher.DispatchJobQueuedAsync(new JobEventPayload
        {
            JobId = jobId,
            ExecutionId = execution.Id,
            EventType = "JobQueued",
            Status = JobStatus.Queued,
            Message = $"Job '{job.Name}' entered the execution queue."
        });

        return Accepted(new
        {
            success = true,
            data = new ExecutionResponse
            {
                Id = execution.Id,
                JobDefinitionId = execution.JobDefinitionId,
                JobName = job.Name,
                HandlerType = job.HandlerType,
                Status = execution.Status,
                Progress = execution.Progress,
                AttemptNumber = execution.AttemptNumber,
                QueuedAt = execution.QueuedAt
            },
            message = "Job execution queued."
        });
    }

    [HttpGet("api/jobs/{jobId:guid}/executions")]
    public async Task<IActionResult> GetJobExecutions(
        Guid jobId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] JobStatus? status = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.JobExecutions
            .Include(e => e.JobDefinition)
            .Where(e => e.JobDefinitionId == jobId)
            .AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(e => e.QueuedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new ExecutionResponse
            {
                Id = e.Id,
                JobDefinitionId = e.JobDefinitionId,
                JobName = e.JobDefinition != null ? e.JobDefinition.Name : string.Empty,
                HandlerType = e.JobDefinition != null ? e.JobDefinition.HandlerType : string.Empty,
                Status = e.Status,
                Progress = e.Progress,
                AttemptNumber = e.AttemptNumber,
                ParentExecutionId = e.ParentExecutionId,
                QueuedAt = e.QueuedAt,
                StartedAt = e.StartedAt,
                CompletedAt = e.CompletedAt,
                WorkerId = e.WorkerId,
                DurationSeconds = e.Duration,
                ErrorMessage = e.ErrorMessage,
                Result = e.Result
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = new PagedResult<ExecutionResponse>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            }
        });
    }

    [HttpGet("api/executions/{id:guid}")]
    public async Task<IActionResult> GetExecutionById(Guid id)
    {
        var execution = await _dbContext.JobExecutions
            .Include(e => e.JobDefinition)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (execution == null)
        {
            return NotFound(new { success = false, message = "Execution not found." });
        }

        var response = new ExecutionResponse
        {
            Id = execution.Id,
            JobDefinitionId = execution.JobDefinitionId,
            JobName = execution.JobDefinition?.Name ?? string.Empty,
            HandlerType = execution.JobDefinition?.HandlerType ?? string.Empty,
            Status = execution.Status,
            Progress = execution.Progress,
            AttemptNumber = execution.AttemptNumber,
            ParentExecutionId = execution.ParentExecutionId,
            QueuedAt = execution.QueuedAt,
            StartedAt = execution.StartedAt,
            CompletedAt = execution.CompletedAt,
            WorkerId = execution.WorkerId,
            DurationSeconds = execution.Duration,
            ErrorMessage = execution.ErrorMessage,
            Result = execution.Result
        };

        return Ok(new { success = true, data = response });
    }

    [HttpGet("api/executions/{id:guid}/logs")]
    public async Task<IActionResult> GetExecutionLogs(Guid id, [FromQuery] long minSequenceNumber = 0)
    {
        var logs = await _dbContext.JobLogs
            .Where(l => l.ExecutionId == id && l.SequenceNumber > minSequenceNumber)
            .OrderBy(l => l.SequenceNumber)
            .AsNoTracking()
            .Select(l => new JobLogResponse
            {
                Id = l.Id,
                ExecutionId = l.ExecutionId,
                Level = l.Level.ToString(),
                Message = l.Message,
                Timestamp = l.Timestamp,
                SequenceNumber = l.SequenceNumber
            })
            .ToListAsync();

        return Ok(new { success = true, data = logs });
    }

    [HttpGet("api/executions/{id:guid}/events")]
    public async Task<IActionResult> GetExecutionEvents(Guid id)
    {
        var events = await _dbContext.JobExecutionEvents
            .Where(ev => ev.ExecutionId == id)
            .OrderBy(ev => ev.Timestamp)
            .AsNoTracking()
            .Select(ev => new JobEventResponse
            {
                Id = ev.Id,
                ExecutionId = ev.ExecutionId,
                PreviousStatus = ev.PreviousStatus,
                NewStatus = ev.NewStatus,
                Reason = ev.Reason,
                Timestamp = ev.Timestamp,
                CorrelationId = ev.CorrelationId
            })
            .ToListAsync();

        return Ok(new { success = true, data = events });
    }

    [HttpPost("api/executions/{id:guid}/cancel")]
    public async Task<IActionResult> CancelExecution(Guid id)
    {
        var execution = await _dbContext.JobExecutions.FindAsync(id);
        if (execution == null)
        {
            return NotFound(new { success = false, message = "Execution not found." });
        }

        if (execution.Status != JobStatus.Queued && execution.Status != JobStatus.Running)
        {
            return BadRequest(new
            {
                success = false,
                message = $"Cannot cancel execution in terminal state '{execution.Status}'."
            });
        }

        if (execution.Status == JobStatus.Queued)
        {
            execution.Status = JobStatus.Cancelled;
            execution.CompletedAt = DateTime.UtcNow;
            execution.ErrorMessage = "Cancelled before execution began.";

            _dbContext.JobExecutionEvents.Add(new JobExecutionEvent
            {
                Id = Guid.NewGuid(),
                ExecutionId = id,
                PreviousStatus = JobStatus.Queued.ToString(),
                NewStatus = JobStatus.Cancelled.ToString(),
                Reason = "Cancelled while queued",
                Timestamp = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid().ToString()
            });

            await _dbContext.SaveChangesAsync();

            await _dispatcher.DispatchJobCancelledAsync(new JobEventPayload
            {
                JobId = execution.JobDefinitionId,
                ExecutionId = id,
                EventType = "JobCancelled",
                Status = JobStatus.Cancelled,
                Message = "Cancelled by user while in queue."
            });

            return Ok(new { success = true, message = "Queued job execution cancelled." });
        }

        // Status is Running: Signal cancellation manager
        execution.Status = JobStatus.CancellationRequested;
        _dbContext.JobExecutionEvents.Add(new JobExecutionEvent
        {
            Id = Guid.NewGuid(),
            ExecutionId = id,
            PreviousStatus = JobStatus.Running.ToString(),
            NewStatus = JobStatus.CancellationRequested.ToString(),
            Reason = "User requested cancellation",
            Timestamp = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid().ToString()
        });

        await _dbContext.SaveChangesAsync();

        _cancellationManager.RequestCancellation(id);

        return Ok(new { success = true, message = "Cancellation requested." });
    }

    [HttpPost("api/executions/{id:guid}/retry")]
    public async Task<IActionResult> RetryExecution(Guid id)
    {
        var previousExecution = await _dbContext.JobExecutions
            .Include(e => e.JobDefinition)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (previousExecution == null)
        {
            return NotFound(new { success = false, message = "Original execution not found." });
        }

        if (previousExecution.Status != JobStatus.Failed && previousExecution.Status != JobStatus.TimedOut)
        {
            return BadRequest(new
            {
                success = false,
                message = $"Only Failed or TimedOut executions can be retried. Current status: '{previousExecution.Status}'."
            });
        }

        var newAttempt = new JobExecution
        {
            Id = Guid.NewGuid(),
            JobDefinitionId = previousExecution.JobDefinitionId,
            Status = JobStatus.Queued,
            Progress = 0,
            AttemptNumber = previousExecution.AttemptNumber + 1,
            ParentExecutionId = previousExecution.Id,
            QueuedAt = DateTime.UtcNow
        };

        _dbContext.JobExecutions.Add(newAttempt);

        _dbContext.JobExecutionEvents.Add(new JobExecutionEvent
        {
            Id = Guid.NewGuid(),
            ExecutionId = newAttempt.Id,
            PreviousStatus = JobStatus.Pending.ToString(),
            NewStatus = JobStatus.Queued.ToString(),
            Reason = $"Retry attempt #{newAttempt.AttemptNumber} triggered for execution {id}",
            Timestamp = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid().ToString()
        });

        await _dbContext.SaveChangesAsync();

        await _dispatcher.DispatchJobQueuedAsync(new JobEventPayload
        {
            JobId = newAttempt.JobDefinitionId,
            ExecutionId = newAttempt.Id,
            EventType = "JobQueued",
            Status = JobStatus.Queued,
            Message = $"Retry attempt #{newAttempt.AttemptNumber} queued."
        });

        return Accepted(new
        {
            success = true,
            data = new ExecutionResponse
            {
                Id = newAttempt.Id,
                JobDefinitionId = newAttempt.JobDefinitionId,
                JobName = previousExecution.JobDefinition?.Name ?? string.Empty,
                Status = newAttempt.Status,
                Progress = newAttempt.Progress,
                AttemptNumber = newAttempt.AttemptNumber,
                ParentExecutionId = newAttempt.ParentExecutionId,
                QueuedAt = newAttempt.QueuedAt
            },
            message = "Retry execution queued."
        });
    }

    [HttpGet("api/executions/active")]
    public async Task<IActionResult> GetActiveExecutions()
    {
        var active = await _dbContext.JobExecutions
            .Include(e => e.JobDefinition)
            .Where(e => e.Status == JobStatus.Queued || e.Status == JobStatus.Running || e.Status == JobStatus.CancellationRequested)
            .OrderByDescending(e => e.QueuedAt)
            .AsNoTracking()
            .Select(e => new ExecutionResponse
            {
                Id = e.Id,
                JobDefinitionId = e.JobDefinitionId,
                JobName = e.JobDefinition != null ? e.JobDefinition.Name : string.Empty,
                HandlerType = e.JobDefinition != null ? e.JobDefinition.HandlerType : string.Empty,
                Status = e.Status,
                Progress = e.Progress,
                AttemptNumber = e.AttemptNumber,
                QueuedAt = e.QueuedAt,
                StartedAt = e.StartedAt,
                WorkerId = e.WorkerId,
                DurationSeconds = e.Duration
            })
            .ToListAsync();

        return Ok(new { success = true, data = active });
    }
}
