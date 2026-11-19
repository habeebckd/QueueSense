using Cronos;
using JobFlow.Core.Entities;
using JobFlow.Core.Enums;

namespace JobFlow.Infrastructure.Services;

public static class JobScheduleCalculator
{
    public static DateTime? CalculateNextRun(JobSchedule schedule, DateTime fromUtc)
    {
        if (!schedule.IsEnabled)
        {
            return null;
        }

        switch (schedule.ScheduleType)
        {
            case ScheduleType.Immediate:
                return fromUtc;

            case ScheduleType.RecurringCron:
                if (string.IsNullOrWhiteSpace(schedule.CronExpression))
                {
                    return null;
                }

                try
                {
                    var expression = CronExpression.Parse(schedule.CronExpression, CronFormat.Standard);
                    var timeZone = TimeZoneInfo.Utc;
                    if (!string.IsNullOrWhiteSpace(schedule.TimeZone) && schedule.TimeZone != "UTC")
                    {
                        try
                        {
                            timeZone = TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZone);
                        }
                        catch
                        {
                            timeZone = TimeZoneInfo.Utc;
                        }
                    }

                    return expression.GetNextOccurrence(fromUtc, timeZone);
                }
                catch
                {
                    return null;
                }

            case ScheduleType.RecurringInterval:
                // Default 1 hour if not specified
                return fromUtc.AddHours(1);

            default:
                return null;
        }
    }
}
