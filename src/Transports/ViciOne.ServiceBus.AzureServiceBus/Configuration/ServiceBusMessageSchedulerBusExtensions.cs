using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Creates and registers schedulers backed by Azure Service Bus scheduled enqueue.</summary>
public static class ServiceBusMessageSchedulerBusExtensions
{
    /// <summary>
    /// Creates a scheduler that sets the Azure Service Bus scheduled enqueue time on outgoing messages.
    /// Use this bus-level scheduler outside a consumer; consumers should schedule through their
    /// <see cref="ConsumeContext"/> so the operation retains consume-scope metadata.
    /// </summary>
    /// <param name="bus">The bus used to resolve send endpoints and topology.</param>
    /// <param name="timeProvider">The optional time source used to calculate delays.</param>
    /// <returns>A scheduler that delegates delayed delivery to Azure Service Bus.</returns>
    public static IMessageScheduler CreateServiceBusMessageScheduler(this IBus bus, TimeProvider? timeProvider = null)
    {
        return new MessageScheduler(new ServiceBusScheduleMessageProvider(bus), bus.Topology, timeProvider);
    }

    /// <summary>
    /// Creates a scheduler that sets the Azure Service Bus scheduled enqueue time on outgoing messages.
    /// Use this provider-level scheduler outside a consumer; consumers should schedule through their
    /// <see cref="ConsumeContext"/> so the operation retains consume-scope metadata.
    /// </summary>
    /// <param name="sendEndpointProvider">The provider used to resolve destination endpoints.</param>
    /// <param name="busTopology">The topology used to obtain publish addresses.</param>
    /// <param name="timeProvider">The optional time source used to calculate delays.</param>
    /// <returns>A scheduler that delegates delayed delivery to Azure Service Bus.</returns>
    public static IMessageScheduler CreateServiceBusMessageScheduler(this ISendEndpointProvider sendEndpointProvider, IBusTopology busTopology, TimeProvider? timeProvider = null)
    {
        return new MessageScheduler(new ServiceBusScheduleMessageProvider(sendEndpointProvider), busTopology, timeProvider);
    }

    /// <summary>Registers a scoped scheduler for the default bus that delegates delayed delivery to Azure Service Bus.</summary>
    /// <param name="configurator">The service registration to update.</param>
    public static void AddServiceBusMessageScheduler(this IRegistrationConfigurator configurator)
    {
        configurator.Services.TryAddScoped(provider =>
        {
            var bus = provider.GetRequiredService<IBus>();
            var sendEndpointProvider = provider.GetRequiredService<ISendEndpointProvider>();
            var timeProvider = provider.GetService<TimeProvider>() ?? TimeProvider.System;
            return sendEndpointProvider.CreateServiceBusMessageScheduler(bus.Topology, timeProvider);
        });
    }

    /// <summary>Registers a scoped scheduler bound to the specified bus that delegates delayed delivery to Azure Service Bus.</summary>
    /// <typeparam name="TBus">The named bus contract.</typeparam>
    /// <param name="configurator">The named bus registration to update.</param>
    public static void AddServiceBusMessageScheduler<TBus>(this IBusRegistrationConfigurator<TBus> configurator)
        where TBus : class, IBus
    {
        configurator.Services.TryAddScoped(provider =>
        {
            var bus = provider.GetRequiredService<TBus>();
            var sendEndpointProvider = provider.GetRequiredService<Bind<TBus, ISendEndpointProvider>>().Value;
            var timeProvider = provider.GetService<TimeProvider>() ?? TimeProvider.System;
            return Bind<TBus>.Create(sendEndpointProvider.CreateServiceBusMessageScheduler(bus.Topology, timeProvider));
        });
    }
}
