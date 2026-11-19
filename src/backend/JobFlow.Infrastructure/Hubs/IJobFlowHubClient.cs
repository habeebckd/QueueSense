using JobFlow.Core.Models;

namespace JobFlow.Infrastructure.Hubs;

public interface IJobFlowHubClient
{
    Task ReceiveJobQueued(JobEventPayload payload);
    Task ReceiveJobStarted(JobEventPayload payload);
    Task ReceiveJobProgressUpdated(JobProgressPayload payload);
    Task ReceiveJobLogAdded(JobLogPayload payload);
    Task ReceiveJobCompleted(JobEventPayload payload);
    Task ReceiveJobFailed(JobEventPayload payload);
    Task ReceiveJobCancelled(JobEventPayload payload);
    Task ReceiveJobTimedOut(JobEventPayload payload);
    Task ReceiveAIAnalysisStarted(JobEventPayload payload);
    Task ReceiveAIAnalysisCompleted(AiAnalysisEventPayload payload);
    Task ReceiveNotification(NotificationPayload payload);
}
