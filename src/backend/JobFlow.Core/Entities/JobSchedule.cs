using JobFlow.Core.Enums;

namespace JobFlow.Core.Entities;

public class JobSchedule
{
    public Guid Id { get; set; }
    public Guid JobDefinitionId { get; set; }
    public ScheduleType ScheduleType { get; set; } = ScheduleType.RecurringCron;
    public string? CronExpression { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public DateTime? NextRunAt { get; set; }
    public bool IsEnabled { get; set; } = true;

    // Navigation property
    public JobDefinition? JobDefinition { get; set; }
}
