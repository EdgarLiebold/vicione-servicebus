using Microsoft.Extensions.Time.Testing;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.QuartzIntegration.Tests.QuartzIntegration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class SchedulerBusObserverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-LIFECYCLE", "starts-and-stops-with-bus")]
    public async Task EnabledScheduler_StartsAndStopsWithTheBus()
    {
        TimeSpan timeout = OperationTimeout();
        ISchedulerFactory schedulerFactory = CreateSchedulerFactory();
        IBusControl? bus = null;

        try
        {
            bus = Bus.Factory.CreateUsingInMemory(configurator =>
                configurator.UseInMemoryScheduler(schedulerFactory, $"quartz-{NewId.NextGuid():N}"));

            await bus.StartAsync(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            IScheduler scheduler = await schedulerFactory.GetScheduler(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);

            Assert.True(scheduler.IsStarted);
            Assert.False(scheduler.InStandbyMode);

            await bus.StopAsync(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            bus = null;

            Assert.True(scheduler.IsShutdown);
        }
        finally
        {
            if (bus is not null)
            {
                await bus.StopAsync(CancellationToken.None)
                    .WaitAsync(timeout, CancellationToken.None);
            }
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-LIFECYCLE", "initializes-without-starting")]
    public async Task DisabledScheduler_IsInitializedButNotStarted()
    {
        TimeSpan timeout = OperationTimeout();
        ISchedulerFactory schedulerFactory = CreateSchedulerFactory();
        IBusControl? bus = null;

        try
        {
            bus = Bus.Factory.CreateUsingInMemory(configurator =>
                configurator.UseInMemoryScheduler(options =>
                {
                    options.SchedulerFactory = schedulerFactory;
                    options.QueueName = $"quartz-{NewId.NextGuid():N}";
                    options.StartScheduler = false;
                }));

            await bus.StartAsync(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            IScheduler scheduler = await schedulerFactory.GetScheduler(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);

            Assert.False(scheduler.IsStarted);
            Assert.True(scheduler.InStandbyMode);

            await bus.StopAsync(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            bus = null;

            Assert.True(scheduler.IsShutdown);
        }
        finally
        {
            if (bus is not null)
            {
                await bus.StopAsync(CancellationToken.None)
                    .WaitAsync(timeout, CancellationToken.None);
            }
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-LIFECYCLE", "job-factory-receives-snapshotted-clock")]
    public async Task CustomJobFactory_ReceivesTheConfiguredBusAndTimeProvider()
    {
        TimeSpan timeout = OperationTimeout();
        ISchedulerFactory schedulerFactory = CreateSchedulerFactory();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2042, 2, 3, 4, 5, 6, TimeSpan.Zero));
        var jobFactory = new RecordingJobFactory();
        IBus? observedBus = null;
        TimeProvider? observedTimeProvider = null;
        IBusControl? bus = null;

        try
        {
            bus = Bus.Factory.CreateUsingInMemory(configurator =>
                configurator.UseInMemoryScheduler(options =>
                {
                    options.SchedulerFactory = schedulerFactory;
                    options.QueueName = $"quartz-{NewId.NextGuid():N}";
                    options.TimeProvider = timeProvider;
                    options.CreateJobFactory = (configuredBus, configuredTimeProvider) =>
                    {
                        observedBus = configuredBus;
                        observedTimeProvider = configuredTimeProvider;
                        return jobFactory;
                    };
                }));

            await bus.StartAsync(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            await schedulerFactory.GetScheduler(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);

            Assert.Same(bus, observedBus);
            Assert.Same(timeProvider, observedTimeProvider);
        }
        finally
        {
            if (bus is not null)
            {
                await bus.StopAsync(CancellationToken.None)
                    .WaitAsync(timeout, CancellationToken.None);
            }
        }
    }

    private static ISchedulerFactory CreateSchedulerFactory()
    {
        return new StdSchedulerFactory(new System.Collections.Specialized.NameValueCollection
        {
            ["quartz.scheduler.instanceName"] = $"ViciOne.ServiceBus.Tests-{NewId.NextGuid():N}",
            ["quartz.threadPool.maxConcurrency"] = "1",
        });
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed class RecordingJobFactory : IJobFactory
    {
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler) => throw new NotSupportedException();

        public void ReturnJob(IJob job)
        {
        }
    }
}
