using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Scheduling;


namespace ViciOne.ServiceBus.Providers.Persistence;

internal static class ReliableSchedulerRegistration
{
    public static void AddStored<TBus>(IServiceCollection services)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(services);
        if (typeof(TBus) == typeof(IBus))
        {
            services.TryAddScoped<IMessageScheduler, ReliableMessageScheduler<TBus>>();
        }
        else
        {
            services.TryAddScoped(provider => Bind<TBus>.Create<IMessageScheduler>(
                new ReliableMessageScheduler<TBus>(
                    provider.GetRequiredService<TBus>(),
                    provider.GetRequiredService<IDurableSender<TBus>>(),
                    provider.GetRequiredService<IScheduleStore<TBus>>(),
                    provider.GetRequiredService<TimeProvider>())));
        }

        services.AddSingleton(provider => Bind<TBus>.Create<IConfigureReceiveEndpoint>(
            new ReliableSchedulerEndpointConfiguration<TBus>(
                provider.GetRequiredService<ReliableSchedulerSelection<TBus>>())));
    }

    public static void ReplaceWithTransport<TBus>(IServiceCollection services)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(services);
        RemoveScheduler<TBus>(services);
        if (typeof(TBus) == typeof(IBus))
        {
            services.AddScoped<IMessageScheduler>(provider =>
            {
                var bus = provider.GetRequiredService<IBus>();
                var sender = provider.GetRequiredService<ISendEndpointProvider>();
                var timeProvider = provider.GetRequiredService<TimeProvider>();
                return sender.CreateDelayedMessageScheduler(bus.Topology, timeProvider);
            });
        }
        else
        {
            services.AddScoped(provider =>
            {
                var bus = provider.GetRequiredService<TBus>();
                var sender = provider.GetRequiredService<Bind<TBus, ISendEndpointProvider>>().Value;
                var timeProvider = provider.GetRequiredService<TimeProvider>();
                return Bind<TBus>.Create<IMessageScheduler>(
                    sender.CreateDelayedMessageScheduler(bus.Topology, timeProvider));
            });
        }
    }

    public static void ReplaceWithEndpoint<TBus>(IServiceCollection services, Uri endpointAddress)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(endpointAddress);
        RemoveScheduler<TBus>(services);
        if (typeof(TBus) == typeof(IBus))
        {
            services.AddScoped<IMessageScheduler>(provider =>
            {
                var bus = provider.GetRequiredService<IBus>();
                var sender = provider.GetRequiredService<ISendEndpointProvider>();
                var timeProvider = provider.GetRequiredService<TimeProvider>();
                return sender.CreateMessageScheduler(bus.Topology, endpointAddress, timeProvider);
            });
            services.TryAddScoped<IRecurringMessageScheduler>(provider =>
            {
                var bus = provider.GetRequiredService<IBus>();
                var sender = provider.GetRequiredService<ISendEndpointProvider>();
                var timeProvider = provider.GetRequiredService<TimeProvider>();
                return new EndpointRecurringMessageScheduler(sender, endpointAddress, bus.Topology, timeProvider);
            });
        }
        else
        {
            services.AddScoped(provider =>
            {
                var bus = provider.GetRequiredService<TBus>();
                var sender = provider.GetRequiredService<Bind<TBus, ISendEndpointProvider>>().Value;
                var timeProvider = provider.GetRequiredService<TimeProvider>();
                return Bind<TBus>.Create<IMessageScheduler>(
                    sender.CreateMessageScheduler(bus.Topology, endpointAddress, timeProvider));
            });
            services.TryAddScoped(provider =>
            {
                var bus = provider.GetRequiredService<TBus>();
                var sender = provider.GetRequiredService<Bind<TBus, ISendEndpointProvider>>().Value;
                var timeProvider = provider.GetRequiredService<TimeProvider>();
                return Bind<TBus>.Create<IRecurringMessageScheduler>(
                    new EndpointRecurringMessageScheduler(sender, endpointAddress, bus.Topology, timeProvider));
            });
        }
    }

    static void RemoveScheduler<TBus>(IServiceCollection services)
        where TBus : class, IBus
    {
        if (typeof(TBus) == typeof(IBus))
            services.RemoveAll<IMessageScheduler>();
        else
            services.RemoveAll<Bind<TBus, IMessageScheduler>>();
    }
}

internal sealed class ReliableSchedulerEndpointConfiguration<TBus>(
    ReliableSchedulerSelection<TBus> selection) :
    IConfigureReceiveEndpoint
    where TBus : class, IBus
{
    readonly ReliableSchedulerSelection<TBus> _selection = selection
        ?? throw new ArgumentNullException(nameof(selection));

    public void Configure(string? name, IReceiveEndpointConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        switch (_selection.Kind)
        {
            case ReliableSchedulerAdapterKind.Store:
            case ReliableSchedulerAdapterKind.Transport:
                // The reliable outbox captures the delay as DueAt for store mode. An explicitly selected
                // transport adapter applies the same filter outside a consume transaction to request native delay.
                configurator.AddPrePipeSpecification(new DelayedMessageSchedulerSpecification());
                break;

            case ReliableSchedulerAdapterKind.Endpoint:
                configurator.AddPrePipeSpecification(new MessageSchedulerPipeSpecification(
                    _selection.EndpointAddress
                    ?? throw new ConfigurationException(
                        global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                            "Scheduling",
                            "unknown",
                            "The selected endpoint scheduler has no address.",
                            "Choose a scheduler adapter with an absolute endpoint address"))));
                break;

            default:
                throw new ConfigurationException(
                    global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                        "Scheduling",
                        "unknown",
                        $"The scheduler adapter value '{_selection.Kind}' is not supported.",
                        "Choose one of the declared reliable-messaging scheduler adapters"));
        }
    }
}
