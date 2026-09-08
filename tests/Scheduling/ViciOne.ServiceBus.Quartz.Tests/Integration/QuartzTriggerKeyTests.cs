using Quartz;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Integration;

public sealed class QuartzTriggerKeyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-TRIGGER-KEY", "complete-user-identity-hash")]
    public void RecurringKey_HashesTheCompleteUserIdentityInsideTheBusNamespace()
    {
        const string schedulerNamespace = "ViciOne.ServiceBus.Quartz.test";
        TriggerKey plain = QuartzTriggerKey.ForRecurring("daily-orders", "operations", schedulerNamespace);
        TriggerKey prefixed = QuartzTriggerKey.ForRecurring(plain.Name, plain.Group, schedulerNamespace);

        Assert.StartsWith(QuartzTriggerKey.RecurringPrefix, plain.Name, StringComparison.Ordinal);
        Assert.Equal(schedulerNamespace, plain.Group);
        Assert.Equal(schedulerNamespace, prefixed.Group);
        Assert.NotEqual(plain, prefixed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-TRIGGER-KEY", "bus-namespace-isolation")]
    public void TriggerKeys_AreIsolatedByBusNamespace()
    {
        TriggerKey first = QuartzTriggerKey.ForRecurring("daily-orders", "operations", "scheduler-a");
        TriggerKey second = QuartzTriggerKey.ForRecurring("daily-orders", "operations", "scheduler-b");
        Guid tokenId = Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f61");

        Assert.NotEqual(first, second);
        Assert.NotEqual(
            QuartzTriggerKey.ForOneTime(tokenId, "scheduler-a"),
            QuartzTriggerKey.ForOneTime(tokenId, "scheduler-b"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [RequirementCoverage("REQ-VSB-QUARTZ-TRIGGER-KEY", "invalid-schedule-id")]
    public void RecurringKey_RejectsMissingScheduleIds(string? scheduleId)
    {
        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(() =>
            QuartzTriggerKey.ForRecurring(scheduleId!, "operations", "scheduler"));

        Assert.Equal("scheduleId", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [RequirementCoverage("REQ-VSB-QUARTZ-TRIGGER-KEY", "invalid-schedule-group")]
    public void RecurringKey_RejectsMissingScheduleGroups(string? scheduleGroup)
    {
        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(() =>
            QuartzTriggerKey.ForRecurring("daily-orders", scheduleGroup!, "scheduler"));

        Assert.Equal("scheduleGroup", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-PARTITION", "composite-identities-cannot-collide")]
    public void PartitionKey_PreservesCompositeIdentityBoundaries()
    {
        string first = QuartzTriggerKey.GetPartitionKey("bc", "a");
        string second = QuartzTriggerKey.GetPartitionKey("c", "ab");

        Assert.NotEqual(first, second);
        Assert.Equal(first, QuartzTriggerKey.GetPartitionKey("bc", "a"));
    }

    [Theory]
    [InlineData(null, "operations", "scheduleId")]
    [InlineData("", "operations", "scheduleId")]
    [InlineData("   ", "operations", "scheduleId")]
    [InlineData("daily-orders", null, "scheduleGroup")]
    [InlineData("daily-orders", "", "scheduleGroup")]
    [InlineData("daily-orders", "   ", "scheduleGroup")]
    [RequirementCoverage("REQ-VSB-QUARTZ-PARTITION", "invalid-composite-identity")]
    public void PartitionKey_RejectsMissingComponents(string? scheduleId, string? scheduleGroup, string parameter)
    {
        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(() =>
            QuartzTriggerKey.GetPartitionKey(scheduleId!, scheduleGroup!));

        Assert.Equal(parameter, exception.ParamName);
    }
}
