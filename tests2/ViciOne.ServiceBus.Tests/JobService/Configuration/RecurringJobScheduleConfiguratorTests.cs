using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Configuration;

public sealed class RecurringJobScheduleConfiguratorTests
{
    [Theory]
    [InlineData(null, null, 5, null, null, null, "0/5 * * * * *")]
    [InlineData(null, null, 5, 12, null, null, "0/5 * 12 * * *")]
    [InlineData(null, null, 5, 12, 15, null, "0/5 15 12 * * *")]
    [InlineData(null, 10, null, null, null, null, "0 0/10 * * * *")]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-SCHEDULE", "every-produces-exact-cron-expression")]
    public void Every_ProducesTheExactCronExpression(
        int? hours,
        int? minutes,
        int? seconds,
        int? hour,
        int? minute,
        int? second,
        string expected)
    {
        var schedule = new RecurringJobScheduleInfo();

        schedule.Every(hours, minutes, seconds, hour, minute, second);

        Assert.Equal(expected, schedule.CronExpression);
        Assert.Empty(schedule.Validate());
    }
}
