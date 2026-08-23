using ViciOne.ServiceBus.Internals.Caching;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Internals.Caching;

public sealed class TimeToLiveCachePolicyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-TTL-CONFIGURATION", "invalid-inputs")]
    public void Construction_RejectsNegativeLifetimeAndNullTimeProvider()
    {
        ArgumentOutOfRangeException negativeLifetime = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TimeToLiveCachePolicy<CacheEntry>(TimeSpan.FromTicks(-1)));
        ArgumentNullException nullTimeProvider = Assert.Throws<ArgumentNullException>(() =>
            new TimeToLiveCachePolicy<CacheEntry>(TimeSpan.Zero, null!));

        Assert.Equal("timeToLive", negativeLifetime.ParamName);
        Assert.Equal("timeProvider", nullTimeProvider.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-CACHE-TTL-CLOCK", "timestamp-frequency")]
    public void Validity_UsesTheTimeProviderTimestampFrequency()
    {
        var timeProvider = new ManualTimeProvider(timestampFrequency: 1_000);
        var policy = new TimeToLiveCachePolicy<CacheEntry>(TimeSpan.FromSeconds(30), timeProvider);
        ITimeToLiveCacheValue<CacheEntry> value = policy.CreateValue(() => { });

        timeProvider.Advance(TimeSpan.FromMilliseconds(29_999));
        Assert.True(policy.IsValid(value));

        timeProvider.Advance(TimeSpan.FromMilliseconds(1));
        Assert.True(policy.IsValid(value));

        timeProvider.Advance(TimeSpan.FromMilliseconds(1));
        Assert.False(policy.IsValid(value));
        Assert.Equal(int.MinValue, policy.CheckValue(value));
    }
}
