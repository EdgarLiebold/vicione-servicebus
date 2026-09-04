using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Configuration;

public sealed class QuartzEndpointDefinitionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-ENDPOINT-SETTINGS", "immutable-snapshot")]
    public void Constructor_SnapshotsEndpointSettings()
    {
        var options = new QuartzEndpointOptions
        {
            QueueName = "scheduled-messages",
            PrefetchCount = 19,
            ConcurrentMessageLimit = 7,
        };
        var definition = new QuartzEndpointDefinition(Options.Create(options));

        options.QueueName = "changed";
        options.PrefetchCount = 2;
        options.ConcurrentMessageLimit = 1;

        Assert.Equal(19, definition.PrefetchCount);
        Assert.Equal(7, definition.ConcurrentMessageLimit);
        Assert.Equal("scheduled-messages", ((IEndpointDefinition)definition).GetEndpointName(DefaultEndpointNameFormatter.Instance));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [RequirementCoverage("REQ-VSB-QUARTZ-ENDPOINT-SETTINGS", "invalid-queue-name")]
    public void Constructor_RejectsMissingQueueName(string? queueName)
    {
        var options = Options.Create(new QuartzEndpointOptions { QueueName = queueName! });

        Assert.ThrowsAny<ArgumentException>(() => new QuartzEndpointDefinition(options));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [RequirementCoverage("REQ-VSB-QUARTZ-ENDPOINT-SETTINGS", "invalid-prefetch-count")]
    public void Constructor_RejectsNonPositivePrefetchCount(int value)
    {
        var options = Options.Create(new QuartzEndpointOptions { PrefetchCount = value });

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new QuartzEndpointDefinition(options));

        Assert.Equal("PrefetchCount", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [RequirementCoverage("REQ-VSB-QUARTZ-ENDPOINT-SETTINGS", "invalid-concurrency-limit")]
    public void Constructor_RejectsNonPositiveConcurrencyLimit(int value)
    {
        var options = Options.Create(new QuartzEndpointOptions { ConcurrentMessageLimit = value });

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new QuartzEndpointDefinition(options));

        Assert.Equal("ConcurrentMessageLimit", exception.ParamName);
    }
}
