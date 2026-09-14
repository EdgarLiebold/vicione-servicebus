using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers transport-native delayed-message schedulers.</summary>
public static class DelayedMessageSchedulerRegistrationExtensions
{
    /// <summary>Registers a scoped scheduler that uses the default bus transport's native delivery delay.</summary>
    /// <param name="configurator">The default bus registration to extend.</param>
    public static void AddDelayedMessageScheduler(this IBusRegistrationConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        configurator.Services.TryAddScoped(provider =>
        {
            var bus = provider.GetRequiredService<IBus>();
            var sendEndpointProvider = provider.GetRequiredService<ISendEndpointProvider>();
            var timeProvider = provider.GetService<TimeProvider>() ?? TimeProvider.System;
            return sendEndpointProvider.CreateDelayedMessageScheduler(bus.Topology, timeProvider);
        });
    }

    /// <summary>Registers a scoped scheduler that uses a typed bus transport's native delivery delay.</summary>
    /// <typeparam name="TBus">The bus contract that owns the scheduler.</typeparam>
    /// <param name="configurator">The typed bus registration to extend.</param>
    public static void AddDelayedMessageScheduler<TBus>(this IBusRegistrationConfigurator<TBus> configurator)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(configurator);

        configurator.Services.TryAddScoped(provider =>
        {
            var bus = provider.GetRequiredService<TBus>();
            var sendEndpointProvider = provider.GetRequiredService<Bind<TBus, ISendEndpointProvider>>().Value;
            var timeProvider = provider.GetService<TimeProvider>() ?? TimeProvider.System;
            return Bind<TBus>.Create(sendEndpointProvider.CreateDelayedMessageScheduler(bus.Topology, timeProvider));
        });
    }
}
