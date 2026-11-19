namespace JobFlow.Core.Entities;

public class JobExecutionEvent
{
    public Guid Id { get; set; }
    public Guid ExecutionId { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = string.Empty;

    // Navigation property
    public JobExecution? Execution { get; set; }
}
