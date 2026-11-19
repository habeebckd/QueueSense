using JobFlow.Core.Enums;

namespace JobFlow.Core.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Operator;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<JobDefinition> CreatedJobs { get; set; } = new List<JobDefinition>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
