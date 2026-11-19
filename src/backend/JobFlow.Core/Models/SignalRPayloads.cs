using JobFlow.Core.Enums;

namespace JobFlow.Core.Models;

public class JobEventPayload
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public Guid ExecutionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public JobStatus Status { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, object?>? Metadata { get; set; }
}

public class JobProgressPayload
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public Guid ExecutionId { get; set; }
    public string EventType { get; set; } = "JobProgressUpdated";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int ProgressPercentage { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Running;
    public string? Message { get; set; }
}

public class JobLogPayload
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid ExecutionId { get; set; }
    public string EventType { get; set; } = "JobLogAdded";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public long SequenceNumber { get; set; }
    public string Level { get; set; } = "Information";
    public string Message { get; set; } = string.Empty;
}

public class AiAnalysisEventPayload
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid ExecutionId { get; set; }
    public string EventType { get; set; } = "AIAnalysisCompleted";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Summary { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium";
    public string Confidence { get; set; } = "Medium";
    public bool RetryRecommended { get; set; }
}

public class NotificationPayload
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ExecutionId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
