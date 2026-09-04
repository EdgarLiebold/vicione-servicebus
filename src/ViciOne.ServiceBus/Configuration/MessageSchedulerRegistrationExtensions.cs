using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides extension methods for message scheduler registration.
/// </summary>
public static class MessageSchedulerRegistrationExtensions
{
    /// <summary>
    /// Add a <see cref="IMessageScheduler" /> to the container that sends <see cref="ScheduleMessage" />
    /// to an external message scheduler on the specified endpoint address, such as Quartz.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="schedulerEndpointAddress">The endpoint address where the scheduler is running</param>
    public static void AddMessageScheduler(this IBusRegistrationConfigurator configurator, Uri schedulerEndpointAddress)
    {
        if (schedulerEndpointAddress == null)
            throw new ArgumentNullException(nameof(schedulerEndpointAddress));

        configurator.Services.TryAddScoped(provider =>
        {
            var bus = provider.GetRequiredService<IBus>();
            var sendEndpointProvider = provider.GetRequiredService<ISendEndpointProvider>();
            var timeProvider = provider.GetService<TimeProvider>() ?? TimeProvider.System;
            return sendEndpointProvider.CreateMessageScheduler(bus.Topology, schedulerEndpointAddress, timeProvider);
        });

        configurator.Services.TryAddScoped<IRecurringMessageScheduler>(provider =>
        {
            var bus = provider.GetRequiredService<IBus>();
            var sendEndpointProvider = provider.GetRequiredService<ISendEndpointProvider>();
            var timeProvider = provider.GetService<TimeProvider>() ?? TimeProvider.System;
            return new EndpointRecurringMessageScheduler(sendEndpointProvider, schedulerEndpointAddress, bus.Topology, timeProvider);
        });
    }

    /// <summary>
    /// Add a <see cref="IMessageScheduler" /> to the container that sends <see cref="ScheduleMessage" />
    /// to an external message scheduler on the specified endpoint address, such as Quartz.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="schedulerEndpointAddress">The endpoint address where the scheduler is running</param>
    public static void AddMessageScheduler<TBus>(this IBusRegistrationConfigurator<TBus> configurator, Uri schedulerEndpointAddress)
        where TBus : class, IBus
    {
        if (schedulerEndpointAddress == null)
            throw new ArgumentNullException(nameof(schedulerEndpointAddress));

        configurator.Services.TryAddScoped(provider =>
        {
            var bus = provider.GetRequiredService<TBus>();
            var sendEndpointProvider = provider.GetRequiredService<Bind<TBus, ISendEndpointProvider>>().Value;
            var timeProvider = provider.GetService<TimeProvider>() ?? TimeProvider.System;
            return Bind<TBus>.Create(sendEndpointProvider.CreateMessageScheduler(bus.Topology, schedulerEndpointAddress, timeProvider));
        });

        configurator.Services.TryAddScoped(provider =>
        {
            var bus = provider.GetRequiredService<TBus>();
            var sendEndpointProvider = provider.GetRequiredService<Bind<TBus, ISendEndpointProvider>>().Value;
            var timeProvider = provider.GetService<TimeProvider>() ?? TimeProvider.System;
            return Bind<TBus>.Create<IRecurringMessageScheduler>(
                new EndpointRecurringMessageScheduler(sendEndpointProvider, schedulerEndpointAddress, bus.Topology, timeProvider));
        });
    }

    /// <summary>
    /// Add a <see cref="IMessageScheduler" /> to the container that publishes <see cref="ScheduleMessage" />
    /// to an external message scheduler, such as Quartz.
    /// </summary>
    /// <param name="configurator"></param>
    public static void AddPublishMessageScheduler(this IBusRegistrationConfigurator configurator)
    {
        configurator.Services.TryAddScoped(provider =>
        {
            var bus = provider.GetRequiredService<IBus>();
            var publishEndpoint = provider.GetRequiredService<IPublishEndpoint>();
            var timeProvider = provider.GetService<TimeProvider>() ?? TimeProvider.System;
            return publishEndpoint.CreateMessageScheduler(bus.Topology, timeProvider);
        });

        configurator.Services.TryAddScoped<IRecurringMessageScheduler>(provider =>
        {
            var bus = provider.GetRequiredService<IBus>();
            var publishEndpoint = provider.GetRequiredService<IPublishEndpoint>();
            var timeProvider = provider.GetService<TimeProvider>() ?? TimeProvider.System;
            return new PublishRecurringMessageScheduler(publishEndpoint, bus.Topology, timeProvider);
        });
    }

    /// <summary>
    /// Add a <see cref="IMessageScheduler" /> to the container that publishes <see cref="ScheduleMessage" />
    /// to an external message scheduler, such as Quartz.
    /// </summary>
    /// <param name="configurator"></param>
    public static void AddPublishMessageScheduler<TBus>(this IBusRegistrationConfigurator<TBus> configurator)
        where TBus : class, IBus
    {
        configurator.Services.TryAddScoped(provider =>
        {
            var bus = provider.GetRequiredService<TBus>();
            var publishEndpoint = provider.GetRequiredService<Bind<TBus, IPublishEndpoint>>().Value;
            var timeProvider = provider.GetService<TimeProvider>() ?? TimeProvider.System;
            return Bind<TBus>.Create(publishEndpoint.CreateMessageScheduler(bus.Topology, timeProvider));
        });

        configurator.Services.TryAddScoped(provider =>
        {
            var bus = provider.GetRequiredService<TBus>();
            var publishEndpoint = provider.GetRequiredService<Bind<TBus, IPublishEndpoint>>().Value;
            var timeProvider = provider.GetService<TimeProvider>() ?? TimeProvider.System;
            return Bind<TBus>.Create<IRecurringMessageScheduler>(new PublishRecurringMessageScheduler(publishEndpoint, bus.Topology, timeProvider));
        });
    }
}
