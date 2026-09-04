using Quartz;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.QuartzIntegration;

public sealed class QuartzTriggerKeyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-TRIGGER-KEY", "canonical-recurring-key")]
    public void RecurringKey_IsCanonicalAndIdempotent()
    {
        TriggerKey plain = QuartzTriggerKey.ForRecurring("daily-orders", "operations");
        TriggerKey canonical = QuartzTriggerKey.ForRecurring(plain.Name, plain.Group);

        Assert.Equal("Recurring.Trigger.daily-orders", plain.Name);
        Assert.Equal("operations", plain.Group);
        Assert.Equal(plain, canonical);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-TRIGGER-KEY", "leading-prefix-only")]
    public void ScheduleId_RemovesOnlyTheLeadingPrefix()
    {
        var key = new TriggerKey("Recurring.Trigger.alpha.Recurring.Trigger.beta", "operations");

        string scheduleId = QuartzTriggerKey.GetScheduleId(key);

        Assert.Equal("alpha.Recurring.Trigger.beta", scheduleId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [RequirementCoverage("REQ-VSB-QUARTZ-TRIGGER-KEY", "invalid-schedule-id")]
    public void RecurringKey_RejectsMissingScheduleIds(string? scheduleId)
    {
        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(() =>
            QuartzTriggerKey.ForRecurring(scheduleId!, "operations"));

        Assert.Equal("scheduleId", exception.ParamName);
    }
}
