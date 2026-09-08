using Quartz;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Configuration;

public sealed class QuartzEndpointOptionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-ENDPOINT-SETTINGS", "coherent-public-defaults")]
    public void Defaults_AreBoundedAndProviderAware()
    {
        var options = new QuartzEndpointOptions();

        Assert.Equal(32, options.PrefetchCount);
        Assert.Null(options.ConcurrentMessageLimit);
        Assert.Equal("quartz", options.QueueName);
        Assert.Null(options.TimeZoneResolver);
        Assert.Null(options.StartDelay);
        Assert.True(options.WaitForJobsToComplete);
        Assert.Equal(
            RetryPolicy.Exponential(5, TimeSpan.FromSeconds(1), 2, TimeSpan.FromMinutes(1)),
            options.DeliveryRetryPolicy);
    }
}
