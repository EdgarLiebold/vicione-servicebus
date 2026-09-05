using Quartz;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Testing;

internal sealed class QuartzTestBus : IAsyncDisposable
{
    private readonly TimeSpan _timeout;

    private QuartzTestBus(IBusControl bus, IScheduler scheduler, ISendEndpoint schedulerEndpoint, TimeSpan timeout)
    {
        Bus = bus;
        Scheduler = scheduler;
        SchedulerEndpoint = schedulerEndpoint;
        _timeout = timeout;
    }

    public IBusControl Bus { get; }
    public IScheduler Scheduler { get; }
    public ISendEndpoint SchedulerEndpoint { get; }

    public static async Task<QuartzTestBus> StartAsync(
        TimeSpan timeout,
        TimeProvider? timeProvider = null,
        Action<IInMemoryBusFactoryConfigurator>? configure = null,
        Func<string, TimeZoneInfo?>? timeZoneResolver = null)
    {
        ISchedulerFactory schedulerFactory = QuartzSchedulerBuilder.Create(builder =>
                builder.UseJobFactory(new ViciOneServiceBusJobFactory()))
            .UseProperties(new System.Collections.Specialized.NameValueCollection
            {
                ["quartz.scheduler.instanceName"] = $"ViciOne.ServiceBus.Tests-{NewId.NextGuid():N}",
                ["quartz.threadPool.maxConcurrency"] = "1",
            })
            .Build();
        Uri? schedulerAddress = null;
        IBusControl bus = global::ViciOne.ServiceBus.Advanced.Bus.Factory.CreateUsingInMemory(configurator =>
        {
            configure?.Invoke(configurator);
            string queueName = $"quartz-{NewId.NextGuid():N}";
            schedulerAddress = timeProvider is null && timeZoneResolver is null
                ? configurator.ConfigureInMemoryScheduler(schedulerFactory, queueName)
                : configurator.ConfigureInMemoryScheduler(options =>
                {
                    options.SchedulerFactory = schedulerFactory;
                    options.QueueName = queueName;
                    options.TimeProvider = timeProvider ?? TimeProvider.System;
                    options.TimeZoneResolver = timeZoneResolver;
                });
        });

        try
        {
            await bus.StartAsync(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            IScheduler scheduler = await schedulerFactory.GetScheduler(TestContext.Current.CancellationToken).AsTask()
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            ISendEndpoint schedulerEndpoint = await bus.GetSendEndpointAsync(Assert.IsType<Uri>(schedulerAddress))
                .WaitAsync(timeout, TestContext.Current.CancellationToken);

            return new QuartzTestBus(bus, scheduler, schedulerEndpoint, timeout);
        }
        catch
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Bus.StopAsync(CancellationToken.None).WaitAsync(_timeout, CancellationToken.None);
    }
}
