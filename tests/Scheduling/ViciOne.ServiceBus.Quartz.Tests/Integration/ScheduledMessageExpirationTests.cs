using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Integration;

public sealed class ScheduledMessageExpirationTests
{
    private static readonly DateTimeOffset Now = new(2042, 2, 3, 4, 5, 6, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-EXPIRATION", "single-time-source")]
    public void Expiration_UsesOnlyTheConfiguredTimeProvider()
    {
        var timeProvider = new FakeTimeProvider(Now);

        TimeSpan? timeToLive = ScheduledMessageExpiration.GetRemainingTimeToLive(
            Now.AddMinutes(30).UtcDateTime,
            timeProvider);

        Assert.Equal(TimeSpan.FromMinutes(30), timeToLive);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-EXPIRATION", "missing-expiration")]
    public void MissingExpiration_LeavesTimeToLiveUnset()
    {
        var timeProvider = new FakeTimeProvider(Now);

        Assert.Null(ScheduledMessageExpiration.GetRemainingTimeToLive(null, timeProvider));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-EXPIRATION", "expired-message")]
    public void PastExpiration_RemainsExpiredInsteadOfReceivingANewLifetime()
    {
        var timeProvider = new FakeTimeProvider(Now);

        TimeSpan? timeToLive = ScheduledMessageExpiration.GetRemainingTimeToLive(
            Now.AddMinutes(-5).UtcDateTime,
            timeProvider);

        Assert.Equal(TimeSpan.FromMinutes(-5), timeToLive);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-EXPIRATION", "invalid-time-provider")]
    public void MissingTimeProvider_FailsFast()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            ScheduledMessageExpiration.GetRemainingTimeToLive(Now.UtcDateTime, null!));

        Assert.Equal("timeProvider", exception.ParamName);
    }
}
