using System.ComponentModel.DataAnnotations;
using JobFlow.Core.Enums;

namespace JobFlow.Core.Models;

public class CreateScheduleRequest
{
    [Required]
    public Guid JobDefinitionId { get; set; }

    [Required]
    public ScheduleType ScheduleType { get; set; } = ScheduleType.RecurringCron;

    public string? CronExpression { get; set; }

    public string TimeZone { get; set; } = "UTC";

    public bool IsEnabled { get; set; } = true;
}

public class UpdateScheduleRequest
{
    [Required]
    public ScheduleType ScheduleType { get; set; } = ScheduleType.RecurringCron;

    public string? CronExpression { get; set; }

    public string TimeZone { get; set; } = "UTC";

    public bool IsEnabled { get; set; } = true;
}

public class ScheduleResponse
{
    public Guid Id { get; set; }
    public Guid JobDefinitionId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public ScheduleType ScheduleType { get; set; }
    public string? CronExpression { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public DateTime? NextRunAt { get; set; }
    public bool IsEnabled { get; set; }
}
