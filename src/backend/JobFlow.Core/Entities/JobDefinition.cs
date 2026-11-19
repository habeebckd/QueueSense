namespace JobFlow.Core.Entities;

public class JobDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string HandlerType { get; set; } = string.Empty;
    public string? Configuration { get; set; }
    public bool IsEnabled { get; set; } = true;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? Creator { get; set; }
    public ICollection<JobSchedule> Schedules { get; set; } = new List<JobSchedule>();
    public ICollection<JobExecution> Executions { get; set; } = new List<JobExecution>();
}
