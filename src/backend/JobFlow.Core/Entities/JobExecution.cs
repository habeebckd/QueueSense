using JobFlow.Core.Enums;

namespace JobFlow.Core.Entities;

public class JobExecution
{
    public Guid Id { get; set; }
    public Guid JobDefinitionId { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public int Progress { get; set; } = 0;
    public int AttemptNumber { get; set; } = 1;
    public Guid? ParentExecutionId { get; set; }
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? WorkerId { get; set; }
    public DateTime? LeaseExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
    public double? Duration { get; set; }
    public string? Result { get; set; }

    // Navigation properties
    public JobDefinition? JobDefinition { get; set; }
    public JobExecution? ParentExecution { get; set; }
    public ICollection<JobExecution> ChildExecutions { get; set; } = new List<JobExecution>();
    public ICollection<JobLog> Logs { get; set; } = new List<JobLog>();
    public ICollection<JobExecutionEvent> Events { get; set; } = new List<JobExecutionEvent>();
    public AIAnalysisResult? AIAnalysisResult { get; set; }
}
