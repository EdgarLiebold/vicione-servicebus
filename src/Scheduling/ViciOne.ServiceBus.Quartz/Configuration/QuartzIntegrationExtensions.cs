using System;
using System.Collections.Specialized;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using ViciOne.ServiceBus.Quartz;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Quartz;

/// <summary>Configures a bus-hosted Quartz scheduler and its in-memory scheduling endpoint.</summary>
public static class QuartzIntegrationExtensions
{

    /// <summary>Creates an isolated in-memory Quartz scheduler and connects it to a bus endpoint.</summary>
    /// <param name="configurator">The bus factory configuration to update.</param>
    /// <param name="queueName">The scheduling endpoint queue name.</param>
    /// <returns>The scheduling endpoint address.</returns>
    public static Uri ConfigureInMemoryScheduler(this IBusFactoryConfigurator configurator, string queueName = "quartz")
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var schedulerFactory = CreateSchedulerFactory();

        return configurator.ConfigureInMemoryScheduler(schedulerFactory, queueName);
    }

    /// <summary>Creates an isolated in-memory Quartz scheduler, returns its factory, and connects it to a bus endpoint.</summary>
    /// <param name="configurator">The bus factory configuration to update.</param>
    /// <param name="schedulerFactory">Receives the created scheduler factory.</param>
    /// <param name="queueName">The scheduling endpoint queue name.</param>
    /// <returns>The scheduling endpoint address.</returns>
    public static Uri ConfigureInMemoryScheduler(this IBusFactoryConfigurator configurator, out ISchedulerFactory schedulerFactory, string queueName = "quartz")
    {
        schedulerFactory = CreateSchedulerFactory();

        return ConfigureInMemoryScheduler(configurator, schedulerFactory, queueName);
    }

    /// <summary>Connects a caller-supplied Quartz scheduler factory to a bus-hosted scheduling endpoint.</summary>
    /// <param name="configurator">The bus factory configuration to update.</param>
    /// <param name="schedulerFactory">The Quartz scheduler factory to use.</param>
    /// <param name="queueName">The scheduling endpoint queue name.</param>
    /// <returns>The scheduling endpoint address.</returns>
    public static Uri ConfigureInMemoryScheduler(this IBusFactoryConfigurator configurator, ISchedulerFactory schedulerFactory, string queueName = "quartz")
    {
        return configurator.ConfigureInMemoryScheduler(options =>
        {
            options.SchedulerFactory = schedulerFactory;

            options.QueueName = queueName;
        });
    }

    static NameValueCollection GetDefaultConfiguration()
    {
        var configuration = new NameValueCollection
        {
            { "quartz.scheduler.instanceName", $"ViciOne.ServiceBus-{NewId.Next().ToString(FormatUtil.Formatter)}" },
            { "quartz.threadPool.maxConcurrency", Environment.ProcessorCount.ToString("F0") }
        };

        return configuration;
    }

    internal static ISchedulerFactory CreateSchedulerFactory()
    {
        return QuartzSchedulerBuilder.Create(builder =>
                builder.UseJobFactory(new ViciOneServiceBusJobFactory()))
            .UseProperties(GetDefaultConfiguration())
            .Build();
    }

    /// <summary>Connects a configured Quartz scheduler to a bus-hosted scheduling endpoint.</summary>
    /// <param name="configurator">The bus factory configuration to update.</param>
    /// <param name="configure">A callback that supplies the scheduler factory, endpoint, clock, and time-zone resolver.</param>
    /// <returns>The scheduling endpoint address.</returns>
    public static Uri ConfigureInMemoryScheduler(this IBusFactoryConfigurator configurator, Action<QuartzSchedulerOptions>? configure)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var options = new QuartzSchedulerOptions();
        configure?.Invoke(options);
        QuartzSchedulerSettings settings = options.CreateSettings();

        Uri? inputAddress = null;

        var observer = new SchedulerBusObserver(settings);

        configurator.ReceiveEndpoint(settings.QueueName, e =>
        {
            var partitioner = configurator.CreatePartitioner(Environment.ProcessorCount);

            e.Consumer(() => new ScheduleMessageConsumer(settings.SchedulerFactory, settings.TimeZoneResolver), x =>
                x.Message<ScheduleMessage>(m => m.UsePartitioner(partitioner, p => p.Message.TokenId)));

            e.Consumer(() => new CancelScheduledMessageConsumer(settings.SchedulerFactory), x =>
                x.Message<CancelScheduledMessage>(m => m.UsePartitioner(partitioner, p => p.Message.TokenId)));

            e.Consumer(() => new PauseScheduledMessageConsumer(settings.SchedulerFactory));

            e.Consumer(() => new ResumeScheduledMessageConsumer(settings.SchedulerFactory));

            configurator.ConfigureMessageScheduler(e.InputAddress);

            configurator.ConnectBusObserver(observer);

            inputAddress = e.InputAddress;
        });

        return inputAddress!;
    }
}
