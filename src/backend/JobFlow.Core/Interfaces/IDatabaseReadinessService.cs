namespace JobFlow.Core.Interfaces;

public interface IDatabaseReadinessService
{
    bool IsReady { get; }
    string Status { get; }
    string? FailureReason { get; }
    DateTime? InitializedAt { get; }
    void SetReady();
    void SetFailed(string reason);
}
