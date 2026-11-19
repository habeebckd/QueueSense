using JobFlow.Core.Interfaces;
using JobFlow.Core.Models;
using JobFlow.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace JobFlow.Infrastructure.Services;

public class SignalRNotificationDispatcher : ISignalRNotificationDispatcher
{
    private readonly IHubContext<JobFlowHub, IJobFlowHubClient> _hubContext;
    private readonly ILogger<SignalRNotificationDispatcher> _logger;

    public SignalRNotificationDispatcher(
        IHubContext<JobFlowHub, IJobFlowHubClient> hubContext,
        ILogger<SignalRNotificationDispatcher> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task DispatchJobQueuedAsync(JobEventPayload payload)
    {
        try
        {
            await _hubContext.Clients.All.ReceiveJobQueued(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching JobQueued event for execution {ExecutionId}", payload.ExecutionId);
        }
    }

    public async Task DispatchJobStartedAsync(JobEventPayload payload)
    {
        try
        {
            await _hubContext.Clients.All.ReceiveJobStarted(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching JobStarted event for execution {ExecutionId}", payload.ExecutionId);
        }
    }

    public async Task DispatchJobProgressUpdatedAsync(JobProgressPayload payload)
    {
        try
        {
            // Broadcast to specific execution group AND all (so dashboard progress updates live)
            await _hubContext.Clients.All.ReceiveJobProgressUpdated(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching JobProgressUpdated event for execution {ExecutionId}", payload.ExecutionId);
        }
    }

    public async Task DispatchJobLogAddedAsync(JobLogPayload payload)
    {
        try
        {
            // Stream to clients subscribed to this execution's group
            await _hubContext.Clients.Group($"execution-{payload.ExecutionId}").ReceiveJobLogAdded(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching JobLogAdded event for execution {ExecutionId}", payload.ExecutionId);
        }
    }

    public async Task DispatchJobCompletedAsync(JobEventPayload payload)
    {
        try
        {
            await _hubContext.Clients.All.ReceiveJobCompleted(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching JobCompleted event for execution {ExecutionId}", payload.ExecutionId);
        }
    }

    public async Task DispatchJobFailedAsync(JobEventPayload payload)
    {
        try
        {
            await _hubContext.Clients.All.ReceiveJobFailed(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching JobFailed event for execution {ExecutionId}", payload.ExecutionId);
        }
    }

    public async Task DispatchJobCancelledAsync(JobEventPayload payload)
    {
        try
        {
            await _hubContext.Clients.All.ReceiveJobCancelled(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching JobCancelled event for execution {ExecutionId}", payload.ExecutionId);
        }
    }

    public async Task DispatchJobTimedOutAsync(JobEventPayload payload)
    {
        try
        {
            await _hubContext.Clients.All.ReceiveJobTimedOut(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching JobTimedOut event for execution {ExecutionId}", payload.ExecutionId);
        }
    }

    public async Task DispatchAIAnalysisStartedAsync(JobEventPayload payload)
    {
        try
        {
            await _hubContext.Clients.All.ReceiveAIAnalysisStarted(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching AIAnalysisStarted event for execution {ExecutionId}", payload.ExecutionId);
        }
    }

    public async Task DispatchAIAnalysisCompletedAsync(AiAnalysisEventPayload payload)
    {
        try
        {
            await _hubContext.Clients.All.ReceiveAIAnalysisCompleted(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching AIAnalysisCompleted event for execution {ExecutionId}", payload.ExecutionId);
        }
    }

    public async Task DispatchNotificationAsync(NotificationPayload payload)
    {
        try
        {
            await _hubContext.Clients.All.ReceiveNotification(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching Notification event {Id}", payload.Id);
        }
    }
}
