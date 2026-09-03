using Microsoft.Extensions.Time.Testing;
using Quartz.Impl;
using Quartz.Spi;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.QuartzIntegration.Tests.Configuration;

public sealed class QuartzSchedulerOptionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-SETTINGS", "immutable-snapshot")]
    public void CreateSettings_SnapshotsEveryRuntimeValue()
    {
        var schedulerFactory = new StdSchedulerFactory();
        var replacementFactory = new StdSchedulerFactory();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2042, 2, 3, 4, 5, 6, TimeSpan.Zero));
        Func<IBus, TimeProvider, IJobFactory> createJobFactory = static (_, _) => throw new NotSupportedException();
        Func<string, TimeZoneInfo?> timeZoneResolver = static _ => TimeZoneInfo.Utc;
        var options = new QuartzSchedulerOptions
        {
            SchedulerFactory = schedulerFactory,
            QueueName = "scheduled-messages",
            CreateJobFactory = createJobFactory,
            StartScheduler = false,
            TimeProvider = timeProvider,
            TimeZoneResolver = timeZoneResolver,
        };

        QuartzSchedulerSettings settings = options.CreateSettings();
        options.SchedulerFactory = replacementFactory;
        options.QueueName = "changed";
        options.CreateJobFactory = null;
        options.StartScheduler = true;
        options.TimeProvider = TimeProvider.System;
        options.TimeZoneResolver = null;

        Assert.Same(schedulerFactory, settings.SchedulerFactory);
        Assert.Equal("scheduled-messages", settings.QueueName);
        Assert.Same(createJobFactory, settings.CreateJobFactory);
        Assert.False(settings.StartScheduler);
        Assert.Same(timeProvider, settings.TimeProvider);
        Assert.Same(timeZoneResolver, settings.TimeZoneResolver);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-SETTINGS", "invalid-scheduler-factory")]
    public void CreateSettings_RejectsMissingSchedulerFactory()
    {
        var options = new QuartzSchedulerOptions { SchedulerFactory = null! };

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(options.CreateSettings);

        Assert.Equal("schedulerFactory", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-SETTINGS", "invalid-queue-name")]
    public void CreateSettings_RejectsMissingQueueName(string? queueName)
    {
        var options = new QuartzSchedulerOptions { QueueName = queueName! };

        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(options.CreateSettings);

        Assert.Equal("queueName", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-SETTINGS", "invalid-time-provider")]
    public void CreateSettings_RejectsMissingTimeProvider()
    {
        var options = new QuartzSchedulerOptions { TimeProvider = null! };

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(options.CreateSettings);

        Assert.Equal("timeProvider", exception.ParamName);
    }
}
