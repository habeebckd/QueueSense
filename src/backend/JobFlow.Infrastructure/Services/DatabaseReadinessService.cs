using JobFlow.Core.Interfaces;

namespace JobFlow.Infrastructure.Services;

public class DatabaseReadinessService : IDatabaseReadinessService
{
    public bool IsReady { get; private set; }
    public string Status { get; private set; } = "Initializing";
    public string? FailureReason { get; private set; }
    public DateTime? InitializedAt { get; private set; }

    public void SetReady()
    {
        IsReady = true;
        Status = "Ready";
        FailureReason = null;
        InitializedAt = DateTime.UtcNow;
    }

    public void SetFailed(string reason)
    {
        IsReady = false;
        Status = "Unhealthy";
        FailureReason = reason;
        InitializedAt = DateTime.UtcNow;
    }
}
