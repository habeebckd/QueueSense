using JobFlow.Core.Enums;

namespace JobFlow.Core.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ExecutionId { get; set; }
    public NotificationType Type { get; set; } = NotificationType.JobQueued;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
    public JobExecution? Execution { get; set; }
}
