namespace JobFlow.Core.Enums;

public enum NotificationType
{
    JobQueued,
    JobStarted,
    JobCompleted,
    JobFailed,
    JobCancelled,
    JobTimedOut,
    AiAnalysisReady
}
