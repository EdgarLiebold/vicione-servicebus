using Quartz;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Quartz.Configuration;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Configuration;

public sealed class QuartzEndpointDefinitionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-ENDPOINT-SETTINGS", "immutable-snapshot")]
    public void Constructor_UsesAnImmutableEndpointSettingsSnapshot()
    {
        Func<string, TimeZoneInfo?> resolver = static _ => TimeZoneInfo.Utc;
        RetryPolicy retryPolicy = RetryPolicy.Fixed(3, TimeSpan.FromSeconds(7));
        var options = new QuartzEndpointOptions
        {
            QueueName = "scheduled-messages",
            PrefetchCount = 19,
            ConcurrentMessageLimit = 7,
            TimeZoneResolver = resolver,
            StartDelay = TimeSpan.FromSeconds(11),
            WaitForJobsToComplete = false,
            DeliveryRetryPolicy = retryPolicy,
        };
        QuartzEndpointSettings settings = options.CreateSettings(typeof(IBus));
        var definition = new QuartzEndpointDefinition<IBus>(settings);

        options.QueueName = "changed";
        options.PrefetchCount = 2;
        options.ConcurrentMessageLimit = 1;
        options.TimeZoneResolver = null;
        options.StartDelay = null;
        options.WaitForJobsToComplete = true;
        options.DeliveryRetryPolicy = RetryPolicy.Fixed(1, TimeSpan.Zero);

        Assert.Equal(19, definition.PrefetchCount);
        Assert.Equal(7, definition.ConcurrentMessageLimit);
        Assert.Same(resolver, settings.TimeZoneResolver);
        Assert.Equal(TimeSpan.FromSeconds(11), settings.StartDelay);
        Assert.False(settings.WaitForJobsToComplete);
        Assert.Equal(retryPolicy, settings.DeliveryRetryPolicy);
        Assert.Equal(QuartzSchedulerNamespace.ForBus(typeof(IBus)), settings.SchedulerNamespace);
        Assert.True(definition.ConfigureConsumeTopology);
        Assert.False(definition.IsTemporary);
        Assert.Equal("scheduled-messages", ((IEndpointDefinition)definition).GetEndpointName(DefaultEndpointNameFormatter.Instance));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [RequirementCoverage("REQ-VSB-QUARTZ-ENDPOINT-SETTINGS", "invalid-queue-name")]
    public void Settings_RejectAMissingQueueName(string? queueName)
    {
        var options = new QuartzEndpointOptions { QueueName = queueName! };

        Assert.ThrowsAny<ArgumentException>(() => options.CreateSettings(typeof(IBus)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [RequirementCoverage("REQ-VSB-QUARTZ-ENDPOINT-SETTINGS", "invalid-prefetch-count")]
    public void Settings_RejectANonPositivePrefetchCount(int value)
    {
        var options = new QuartzEndpointOptions { PrefetchCount = value };

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            options.CreateSettings(typeof(IBus)));

        Assert.Equal("PrefetchCount", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [RequirementCoverage("REQ-VSB-QUARTZ-ENDPOINT-SETTINGS", "invalid-concurrency-limit")]
    public void Settings_RejectANonPositiveConcurrencyLimit(int value)
    {
        var options = new QuartzEndpointOptions { ConcurrentMessageLimit = value };

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            options.CreateSettings(typeof(IBus)));

        Assert.Equal("ConcurrentMessageLimit", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-ENDPOINT-SETTINGS", "partitioner-owned-and-disposed")]
    public async Task DisposeAsync_ReleasesTheOwnedPartitionerAsync()
    {
        var definition = new QuartzEndpointDefinition<IBus>(
            new QuartzEndpointOptions().CreateSettings(typeof(IBus)));

        await definition.DisposeAsync();
    }
}
