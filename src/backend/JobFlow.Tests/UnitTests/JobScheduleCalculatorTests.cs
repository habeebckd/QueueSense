using JobFlow.Core.Entities;
using JobFlow.Core.Enums;
using JobFlow.Infrastructure.Services;
using Xunit;

namespace JobFlow.Tests.UnitTests;

public class JobScheduleCalculatorTests
{
    [Fact]
    public void CalculateNextRun_Immediate_ReturnsCurrentUtc()
    {
        var schedule = new JobSchedule
        {
            ScheduleType = ScheduleType.Immediate,
            IsEnabled = true
        };

        var now = new DateTime(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc);
        var next = JobScheduleCalculator.CalculateNextRun(schedule, now);

        Assert.Equal(now, next);
    }

    [Fact]
    public void CalculateNextRun_RecurringCron_ReturnsValidFutureOccurrence()
    {
        var schedule = new JobSchedule
        {
            ScheduleType = ScheduleType.RecurringCron,
            CronExpression = "0 * * * *", // Top of every hour
            TimeZone = "UTC",
            IsEnabled = true
        };

        var now = new DateTime(2026, 11, 11, 12, 15, 0, DateTimeKind.Utc);
        var next = JobScheduleCalculator.CalculateNextRun(schedule, now);

        Assert.NotNull(next);
        Assert.Equal(new DateTime(2026, 11, 11, 13, 0, 0, DateTimeKind.Utc), next.Value);
    }

    [Fact]
    public void CalculateNextRun_Disabled_ReturnsNull()
    {
        var schedule = new JobSchedule
        {
            ScheduleType = ScheduleType.RecurringCron,
            CronExpression = "0 * * * *",
            IsEnabled = false
        };

        var now = DateTime.UtcNow;
        var next = JobScheduleCalculator.CalculateNextRun(schedule, now);

        Assert.Null(next);
    }
}
