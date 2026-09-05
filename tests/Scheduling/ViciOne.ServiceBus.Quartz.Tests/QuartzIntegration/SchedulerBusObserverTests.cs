using Microsoft.Extensions.Time.Testing;
using Quartz;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.QuartzIntegration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class SchedulerBusObserverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-LIFECYCLE", "starts-and-stops-with-bus")]
    public async Task EnabledScheduler_StartsAndStopsWithTheBusAsync()
    {
        TimeSpan timeout = OperationTimeout();
        ISchedulerFactory schedulerFactory = CreateSchedulerFactory();
        IBusControl? bus = null;

        try
        {
            bus = Bus.Factory.CreateUsingInMemory(configurator =>
                configurator.ConfigureInMemoryScheduler(schedulerFactory, $"quartz-{NewId.NextGuid():N}"));

            await bus.StartAsync(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            IScheduler scheduler = await schedulerFactory.GetScheduler(TestContext.Current.CancellationToken).AsTask()
                .WaitAsync(timeout, TestContext.Current.CancellationToken);

            Assert.Equal(SchedulerStatus.Running, scheduler.Status);

            await bus.StopAsync(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            bus = null;

            Assert.Equal(SchedulerStatus.Shutdown, scheduler.Status);
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
    public async Task DisabledScheduler_IsInitializedButNotStartedAsync()
    {
        TimeSpan timeout = OperationTimeout();
        ISchedulerFactory schedulerFactory = CreateSchedulerFactory();
        IBusControl? bus = null;

        try
        {
            bus = Bus.Factory.CreateUsingInMemory(configurator =>
                configurator.ConfigureInMemoryScheduler(options =>
                {
                    options.SchedulerFactory = schedulerFactory;
                    options.QueueName = $"quartz-{NewId.NextGuid():N}";
                    options.StartScheduler = false;
                }));

            await bus.StartAsync(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            IScheduler scheduler = await schedulerFactory.GetScheduler(TestContext.Current.CancellationToken).AsTask()
                .WaitAsync(timeout, TestContext.Current.CancellationToken);

            Assert.Equal(SchedulerStatus.Created, scheduler.Status);

            await bus.StopAsync(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            bus = null;

            Assert.Equal(SchedulerStatus.Shutdown, scheduler.Status);
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
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-LIFECYCLE", "standalone-context-receives-snapshotted-clock")]
    public async Task StandaloneContext_ReceivesTheConfiguredBusAndTimeProviderAsync()
    {
        TimeSpan timeout = OperationTimeout();
        ISchedulerFactory schedulerFactory = CreateSchedulerFactory();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2042, 2, 3, 4, 5, 6, TimeSpan.Zero));
        IBusControl? bus = null;

        try
        {
            bus = Bus.Factory.CreateUsingInMemory(configurator =>
                configurator.ConfigureInMemoryScheduler(options =>
                {
                    options.SchedulerFactory = schedulerFactory;
                    options.QueueName = $"quartz-{NewId.NextGuid():N}";
                    options.TimeProvider = timeProvider;
                }));

            await bus.StartAsync(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            IScheduler scheduler = await schedulerFactory.GetScheduler(TestContext.Current.CancellationToken).AsTask()
                .WaitAsync(timeout, TestContext.Current.CancellationToken);

            Assert.Same(bus, scheduler.Context[ScheduledMessageJob.BusContextKey]);
            Assert.Same(timeProvider, scheduler.Context[ScheduledMessageJob.TimeProviderContextKey]);
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
        return QuartzSchedulerBuilder.Create(builder =>
                builder.UseJobFactory(new ViciOneServiceBusJobFactory()))
            .UseProperties(new System.Collections.Specialized.NameValueCollection
            {
                ["quartz.scheduler.instanceName"] = $"ViciOne.ServiceBus.Tests-{NewId.NextGuid():N}",
                ["quartz.threadPool.maxConcurrency"] = "1",
            })
            .Build();
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

}
