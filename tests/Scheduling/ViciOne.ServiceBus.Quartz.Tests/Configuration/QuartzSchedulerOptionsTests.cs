using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using Quartz;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Configuration;

public sealed class QuartzSchedulerOptionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-SETTINGS", "coherent-public-defaults")]
    public void Defaults_StartWithTheBusAndUseTheSystemClock()
    {
        var options = new QuartzSchedulerOptions();

        Assert.Equal(32, options.PrefetchCount);
        Assert.Null(options.ConcurrentMessageLimit);
        Assert.Equal("quartz", options.QueueName);
        Assert.True(options.StartScheduler);
        Assert.Null(options.StartDelay);
        Assert.True(options.WaitForJobsToComplete);
        Assert.Same(TimeProvider.System, options.TimeProvider);
        Assert.Null(options.TimeZoneResolver);
        Assert.Equal(
            RetryPolicy.Exponential(5, TimeSpan.FromSeconds(1), 2, TimeSpan.FromMinutes(1)),
            options.DeliveryRetryPolicy);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-SETTINGS", "immutable-snapshot")]
    public void CreateSettings_SnapshotsEveryRuntimeValue()
    {
        ISchedulerFactory schedulerFactory = DispatchProxy.Create<ISchedulerFactory, NoOpDispatchProxy>();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2042, 2, 3, 4, 5, 6, TimeSpan.Zero));
        Func<string, TimeZoneInfo?> timeZoneResolver = static _ => TimeZoneInfo.Utc;
        RetryPolicy retryPolicy = RetryPolicy.Fixed(4, TimeSpan.FromSeconds(3));
        var options = new QuartzSchedulerOptions
        {
            QueueName = "scheduled-messages",
            PrefetchCount = 19,
            ConcurrentMessageLimit = 7,
            StartScheduler = false,
            StartDelay = TimeSpan.FromSeconds(11),
            WaitForJobsToComplete = false,
            TimeProvider = timeProvider,
            TimeZoneResolver = timeZoneResolver,
            DeliveryRetryPolicy = retryPolicy,
        };

        QuartzSchedulerSettings settings = options.CreateSettings(schedulerFactory);
        options.QueueName = "changed";
        options.PrefetchCount = 2;
        options.ConcurrentMessageLimit = 1;
        options.StartScheduler = true;
        options.StartDelay = null;
        options.WaitForJobsToComplete = true;
        options.TimeProvider = TimeProvider.System;
        options.TimeZoneResolver = null;
        options.DeliveryRetryPolicy = RetryPolicy.Fixed(1, TimeSpan.Zero);

        Assert.Same(schedulerFactory, settings.SchedulerFactory);
        Assert.Equal("scheduled-messages", settings.QueueName);
        Assert.Equal(19, settings.PrefetchCount);
        Assert.Equal(7, settings.ConcurrentMessageLimit);
        Assert.False(settings.StartScheduler);
        Assert.Equal(TimeSpan.FromSeconds(11), settings.StartDelay);
        Assert.False(settings.WaitForJobsToComplete);
        Assert.Same(timeProvider, settings.TimeProvider);
        Assert.Same(timeZoneResolver, settings.TimeZoneResolver);
        Assert.Equal(retryPolicy, settings.DeliveryRetryPolicy);
        Assert.Equal(QuartzSchedulerNamespace.ForEndpoint("scheduled-messages"), settings.SchedulerNamespace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-SETTINGS", "invalid-scheduler-factory")]
    public void CreateSettings_RejectsMissingSchedulerFactory()
    {
        var options = new QuartzSchedulerOptions();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => options.CreateSettings(null!));

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

        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(() =>
            options.CreateSettings(QuartzSchedulerBuilder.Create().Build()));

        Assert.Equal(nameof(QuartzSchedulerOptions.QueueName), exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-SETTINGS", "invalid-direct-prefetch-count")]
    public void CreateSettings_RejectsANonPositivePrefetchCount(int value)
    {
        var options = new QuartzSchedulerOptions { PrefetchCount = value };

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            options.CreateSettings(QuartzSchedulerBuilder.Create().Build()));

        Assert.Equal(nameof(QuartzSchedulerOptions.PrefetchCount), exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-SETTINGS", "invalid-direct-concurrency-limit")]
    public void CreateSettings_RejectsANonPositiveConcurrencyLimit(int value)
    {
        var options = new QuartzSchedulerOptions { ConcurrentMessageLimit = value };

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            options.CreateSettings(QuartzSchedulerBuilder.Create().Build()));

        Assert.Equal(nameof(QuartzSchedulerOptions.ConcurrentMessageLimit), exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-SETTINGS", "invalid-direct-start-delay")]
    public void CreateSettings_RejectsANegativeStartDelay()
    {
        var options = new QuartzSchedulerOptions { StartDelay = TimeSpan.FromTicks(-1) };

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            options.CreateSettings(QuartzSchedulerBuilder.Create().Build()));

        Assert.Equal(nameof(QuartzSchedulerOptions.StartDelay), exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-SETTINGS", "invalid-time-provider")]
    public void CreateSettings_RejectsMissingTimeProvider()
    {
        var options = new QuartzSchedulerOptions { TimeProvider = null! };

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            options.CreateSettings(QuartzSchedulerBuilder.Create().Build()));

        Assert.Equal(nameof(QuartzSchedulerOptions.TimeProvider), exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-SETTINGS", "invalid-retry-policy")]
    public void CreateSettings_RejectsMissingRetryPolicy()
    {
        var options = new QuartzSchedulerOptions { DeliveryRetryPolicy = null! };

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            options.CreateSettings(QuartzSchedulerBuilder.Create().Build()));

        Assert.Equal(nameof(QuartzSchedulerOptions.DeliveryRetryPolicy), exception.ParamName);
    }

    [Theory]
    [InlineData(InvalidRuntimeSetting.SchedulerFactory, "schedulerFactory")]
    [InlineData(InvalidRuntimeSetting.QueueName, "queueName")]
    [InlineData(InvalidRuntimeSetting.PrefetchCount, "prefetchCount")]
    [InlineData(InvalidRuntimeSetting.ConcurrentMessageLimit, "concurrentMessageLimit")]
    [InlineData(InvalidRuntimeSetting.StartDelay, "startDelay")]
    [InlineData(InvalidRuntimeSetting.TimeProvider, "timeProvider")]
    [InlineData(InvalidRuntimeSetting.DeliveryRetryPolicy, "deliveryRetryPolicy")]
    [InlineData(InvalidRuntimeSetting.SchedulerNamespace, "schedulerNamespace")]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-SETTINGS", "immutable-runtime-settings-defend-their-invariants")]
    public void RuntimeSettings_RejectInvalidConstruction(InvalidRuntimeSetting invalid, string expectedParameter)
    {
        ISchedulerFactory schedulerFactory = DispatchProxy.Create<ISchedulerFactory, NoOpDispatchProxy>();

        ArgumentException failure = Assert.ThrowsAny<ArgumentException>(() => new QuartzSchedulerSettings(
            invalid == InvalidRuntimeSetting.SchedulerFactory ? null! : schedulerFactory,
            invalid == InvalidRuntimeSetting.QueueName ? " " : "quartz",
            invalid == InvalidRuntimeSetting.PrefetchCount ? 0 : 32,
            invalid == InvalidRuntimeSetting.ConcurrentMessageLimit ? 0 : 4,
            startScheduler: true,
            invalid == InvalidRuntimeSetting.StartDelay ? TimeSpan.FromTicks(-1) : TimeSpan.Zero,
            waitForJobsToComplete: true,
            invalid == InvalidRuntimeSetting.TimeProvider ? null! : TimeProvider.System,
            timeZoneResolver: null,
            invalid == InvalidRuntimeSetting.DeliveryRetryPolicy ? null! : RetryPolicy.Fixed(1, TimeSpan.Zero),
            invalid == InvalidRuntimeSetting.SchedulerNamespace ? " " : "quartz-tests"));

        Assert.Equal(expectedParameter, failure.ParamName);
    }

    public enum InvalidRuntimeSetting
    {
        SchedulerFactory,
        QueueName,
        PrefetchCount,
        ConcurrentMessageLimit,
        StartDelay,
        TimeProvider,
        DeliveryRetryPolicy,
        SchedulerNamespace,
    }

    private class NoOpDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }
}
