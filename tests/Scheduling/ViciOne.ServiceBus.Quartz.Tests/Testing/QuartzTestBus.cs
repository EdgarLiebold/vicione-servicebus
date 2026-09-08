using Quartz;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Quartz.Runtime;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Testing;

internal sealed class QuartzTestBus : IAsyncDisposable
{
    private readonly TimeSpan _timeout;
    private readonly QuartzSchedulerLease _schedulerLease;

    private QuartzTestBus(
        IBusControl bus,
        IScheduler scheduler,
        ISendEndpoint schedulerEndpoint,
        QuartzSchedulerLease schedulerLease,
        string queueName,
        string schedulerNamespace,
        TimeSpan timeout)
    {
        Bus = bus;
        Scheduler = scheduler;
        SchedulerEndpoint = schedulerEndpoint;
        _schedulerLease = schedulerLease;
        QueueName = queueName;
        SchedulerNamespace = schedulerNamespace;
        _timeout = timeout;
    }

    public IBusControl Bus { get; }
    public IScheduler Scheduler { get; }
    public ISendEndpoint SchedulerEndpoint { get; }
    public string QueueName { get; }
    public string SchedulerNamespace { get; }

    public static async Task<QuartzTestBus> StartAsync(
        TimeSpan timeout,
        TimeProvider? timeProvider = null,
        Action<IInMemoryBusFactoryConfigurator>? configure = null,
        Func<string, TimeZoneInfo?>? timeZoneResolver = null,
        Action<QuartzSchedulerOptions>? configureScheduler = null)
    {
        QuartzSchedulerLease? schedulerLease = null;
        string? configuredQueueName = null;
        string? schedulerNamespace = null;
        IBusControl bus = global::ViciOne.ServiceBus.Advanced.Bus.Factory.CreateUsingInMemory(configurator =>
        {
            configure?.Invoke(configurator);
            string queueName = $"quartz-{NewId.NextGuid():N}";
            schedulerLease = configurator.ConfigureInMemoryQuartzScheduler(options =>
            {
                options.QueueName = queueName;
                options.TimeProvider = timeProvider ?? TimeProvider.System;
                options.TimeZoneResolver = timeZoneResolver;
                configureScheduler?.Invoke(options);
                configuredQueueName = options.QueueName;
                schedulerNamespace = QuartzSchedulerNamespace.ForEndpoint(options.QueueName);
            });
        });

        try
        {
            await bus.StartAsync(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            QuartzSchedulerLease configuredLease = Assert.IsType<QuartzSchedulerLease>(schedulerLease);
            ISchedulerFactory configuredFactory = configuredLease.SchedulerFactory;
            IScheduler scheduler = await configuredFactory.GetScheduler(TestContext.Current.CancellationToken).AsTask()
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            ISendEndpoint schedulerEndpoint = await bus.GetSendEndpointAsync(configuredLease.EndpointAddress)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);

            return new QuartzTestBus(
                bus,
                scheduler,
                schedulerEndpoint,
                configuredLease,
                Assert.IsType<string>(configuredQueueName),
                Assert.IsType<string>(schedulerNamespace),
                timeout);
        }
        catch
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            if (schedulerLease is not null)
                await schedulerLease.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Bus.StopAsync(CancellationToken.None).WaitAsync(_timeout, CancellationToken.None);
        await _schedulerLease.DisposeAsync();
    }
}
