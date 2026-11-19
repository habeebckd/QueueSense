using JobFlow.Core.Models;

namespace JobFlow.Core.Interfaces;

public interface ISignalRNotificationDispatcher
{
    Task DispatchJobQueuedAsync(JobEventPayload payload);
    Task DispatchJobStartedAsync(JobEventPayload payload);
    Task DispatchJobProgressUpdatedAsync(JobProgressPayload payload);
    Task DispatchJobLogAddedAsync(JobLogPayload payload);
    Task DispatchJobCompletedAsync(JobEventPayload payload);
    Task DispatchJobFailedAsync(JobEventPayload payload);
    Task DispatchJobCancelledAsync(JobEventPayload payload);
    Task DispatchJobTimedOutAsync(JobEventPayload payload);
    Task DispatchAIAnalysisStartedAsync(JobEventPayload payload);
    Task DispatchAIAnalysisCompletedAsync(AiAnalysisEventPayload payload);
    Task DispatchNotificationAsync(NotificationPayload payload);
}
