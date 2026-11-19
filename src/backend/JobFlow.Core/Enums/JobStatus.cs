namespace JobFlow.Core.Enums;

public enum JobStatus
{
    Pending,
    Queued,
    Running,
    Completed,
    Failed,
    TimedOut,
    CancellationRequested,
    Cancelled
}
