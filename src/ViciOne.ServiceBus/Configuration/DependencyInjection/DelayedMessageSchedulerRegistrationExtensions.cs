using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for delayed message scheduler registration.</summary>
public static class DelayedMessageSchedulerRegistrationExtensions
{
    /// <summary>Add a <see cref="IMessageScheduler" /> to the container that uses transport message delay to schedule messages.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public static void AddDelayedMessageScheduler(this IBusRegistrationConfigurator configurator)
    {
        configurator.Services.TryAddScoped(provider =>
        {
            var bus = provider.GetRequiredService<IBus>();
            var sendEndpointProvider = provider.GetRequiredService<ISendEndpointProvider>();
            var timeProvider = provider.GetService<TimeProvider>() ?? TimeProvider.System;
            return sendEndpointProvider.CreateDelayedMessageScheduler(bus.Topology, timeProvider);
        });
    }

    /// <summary>Add a <see cref="IMessageScheduler" /> to the container that uses transport message delay to schedule messages.</summary>
    /// <typeparam name="TBus">The bus type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public static void AddDelayedMessageScheduler<TBus>(this IBusRegistrationConfigurator<TBus> configurator)
        where TBus : class, IBus
    {
        configurator.Services.TryAddScoped(provider =>
        {
            var bus = provider.GetRequiredService<TBus>();
            var sendEndpointProvider = provider.GetRequiredService<Bind<TBus, ISendEndpointProvider>>().Value;
            var timeProvider = provider.GetService<TimeProvider>() ?? TimeProvider.System;
            return Bind<TBus>.Create(sendEndpointProvider.CreateDelayedMessageScheduler(bus.Topology, timeProvider));
        });
    }
}
