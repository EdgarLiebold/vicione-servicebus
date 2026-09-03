using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Configuration;

public sealed class JobConsumerTimeProviderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CLOCK", "options-clock-owner-and-null-boundary")]
    public void Options_DefaultInjectAndNullPathsHaveOneExplicitClockOwner()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2043, 2, 3, 4, 5, 6, TimeSpan.Zero));
        var options = new JobConsumerOptions();

        Assert.Same(TimeProvider.System, options.TimeProvider);
        Assert.Same(options, options.SetTimeProvider(clock));
        Assert.Same(clock, options.TimeProvider);
        Assert.Equal("timeProvider", Assert.Throws<ArgumentNullException>(() => options.SetTimeProvider(null!)).ParamName);
    }
}
