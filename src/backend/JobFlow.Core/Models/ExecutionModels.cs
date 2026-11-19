using JobFlow.Core.Enums;

namespace JobFlow.Core.Models;

public class RunJobRequest
{
    public string? ConfigurationOverrides { get; set; }
}

public class ExecutionResponse
{
    public Guid Id { get; set; }
    public Guid JobDefinitionId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public string HandlerType { get; set; } = string.Empty;
    public JobStatus Status { get; set; }
    public int Progress { get; set; }
    public int AttemptNumber { get; set; }
    public Guid? ParentExecutionId { get; set; }
    public DateTime QueuedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? WorkerId { get; set; }
    public double? DurationSeconds { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Result { get; set; }
}

public class JobLogResponse
{
    public Guid Id { get; set; }
    public Guid ExecutionId { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public long SequenceNumber { get; set; }
}

public class JobEventResponse
{
    public Guid Id { get; set; }
    public Guid ExecutionId { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
