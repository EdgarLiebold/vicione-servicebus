using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Transactions;

namespace ViciOne.ServiceBus;

public static class DependencyInjectionTransactionExtensions
{
    /// <summary>
    /// Adds a singleton <see cref="IAmbientTransactionBus" /> for the default bus. Sends and publishes are dispatched immediately when no
    /// ambient transaction exists and are deferred to the prepare phase while <see cref="System.Transactions.Transaction.Current" /> is active.
    /// This capability is best-effort and is not a durable atomic outbox.
    /// </summary>
    public static void AddAmbientTransactionBus(this IBusRegistrationConfigurator busConfigurator)
    {
        if (busConfigurator == null)
            throw new ArgumentNullException(nameof(busConfigurator));

        EnsureCompatible<IBus, IBufferedBus>(busConfigurator, nameof(AddAmbientTransactionBus));
        EnsureScopedContextOwner<IBus, AmbientTransactionScopedBusContextProvider<IBus>>(
            busConfigurator,
            nameof(AddAmbientTransactionBus));

        busConfigurator.TryAddSingleton<IAmbientTransactionBus>(provider =>
            new AmbientTransactionBus(provider.GetRequiredService<IBus>()));
        busConfigurator.TryAddSingleton(provider =>
            Bind<IBus>.Create(provider.GetRequiredService<IAmbientTransactionBus>()));

        busConfigurator.ReplaceScoped<IScopedBusContextProvider<IBus>, AmbientTransactionScopedBusContextProvider<IBus>>();
    }

    /// <summary>
    /// Adds a singleton ambient-transaction capability bound to the specified bus instance.
    /// </summary>
    public static void AddAmbientTransactionBus<TBus>(this IBusRegistrationConfigurator<TBus> busConfigurator)
        where TBus : class, IBus
    {
        if (busConfigurator == null)
            throw new ArgumentNullException(nameof(busConfigurator));

        EnsureCompatible<TBus, IBufferedBus>(busConfigurator, nameof(AddAmbientTransactionBus));
        EnsureScopedContextOwner<TBus, AmbientTransactionScopedBusContextProvider<TBus>>(
            busConfigurator,
            nameof(AddAmbientTransactionBus));

        busConfigurator.TryAddSingleton(provider =>
            Bind<TBus>.Create<IAmbientTransactionBus>(new AmbientTransactionBus(provider.GetRequiredService<TBus>())));

        busConfigurator.ReplaceScoped<IScopedBusContextProvider<TBus>, AmbientTransactionScopedBusContextProvider<TBus>>();
    }

    /// <summary>
    /// Adds a scoped <see cref="IBufferedBus" /> for the default bus. Each scope owns an in-memory FIFO buffer that is dispatched only by
    /// <see cref="IBufferedBus.FlushAsync" />. This capability is not durable and is not an atomic outbox.
    /// </summary>
    public static void AddBufferedBus(this IBusRegistrationConfigurator busConfigurator, int capacity = BufferedBus.DefaultCapacity)
    {
        if (busConfigurator == null)
            throw new ArgumentNullException(nameof(busConfigurator));
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Buffered bus capacity must be greater than zero.");

        EnsureCompatible<IBus, IAmbientTransactionBus>(busConfigurator, nameof(AddBufferedBus));
        EnsureScopedContextOwner<IBus, BufferedBusScopedBusContextProvider<IBus>>(
            busConfigurator,
            nameof(AddBufferedBus));
        EnsureBufferedCapacity<IBus>(busConfigurator, capacity);

        busConfigurator.TryAddScoped<IBufferedBus>(provider => new BufferedBus(provider.GetRequiredService<IBus>(), capacity));
        busConfigurator.TryAddScoped(provider => Bind<IBus>.Create(provider.GetRequiredService<IBufferedBus>()));

        busConfigurator.ReplaceScoped<IScopedBusContextProvider<IBus>, BufferedBusScopedBusContextProvider<IBus>>();
    }

    /// <summary>
    /// Adds a scoped explicitly buffered capability bound to the specified bus instance.
    /// </summary>
    public static void AddBufferedBus<TBus>(this IBusRegistrationConfigurator<TBus> busConfigurator,
        int capacity = BufferedBus.DefaultCapacity)
        where TBus : class, IBus
    {
        if (busConfigurator == null)
            throw new ArgumentNullException(nameof(busConfigurator));
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Buffered bus capacity must be greater than zero.");

        EnsureCompatible<TBus, IAmbientTransactionBus>(busConfigurator, nameof(AddBufferedBus));
        EnsureScopedContextOwner<TBus, BufferedBusScopedBusContextProvider<TBus>>(
            busConfigurator,
            nameof(AddBufferedBus));
        EnsureBufferedCapacity<TBus>(busConfigurator, capacity);

        busConfigurator.TryAddScoped(provider =>
            Bind<TBus>.Create<IBufferedBus>(new BufferedBus(provider.GetRequiredService<TBus>(), capacity)));

        busConfigurator.ReplaceScoped<IScopedBusContextProvider<TBus>, BufferedBusScopedBusContextProvider<TBus>>();
    }

    static void EnsureBufferedCapacity<TBus>(IServiceCollection services, int capacity)
        where TBus : class, IBus
    {
        ServiceDescriptor? existing = services.FirstOrDefault(descriptor =>
            descriptor.ServiceType == typeof(BufferedCapacity<TBus>));
        if (existing?.ImplementationInstance is BufferedCapacity<TBus> configured)
        {
            if (configured.Value != capacity)
            {
                throw new ConfigurationException(
                    $"AddBufferedBus for {TypeCache<TBus>.ShortName} was already configured with capacity {configured.Value} and cannot be changed to {capacity}.");
            }

            return;
        }

        services.AddSingleton(new BufferedCapacity<TBus>(capacity));
    }

    static void EnsureCompatible<TBus, TConflictingCapability>(IServiceCollection services, string registration)
        where TBus : class, IBus
        where TConflictingCapability : class, IBus
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(Bind<TBus, TConflictingCapability>)))
        {
            throw new ConfigurationException(
                $"{registration} cannot be combined with {TypeCache<TConflictingCapability>.ShortName} for {TypeCache<TBus>.ShortName}.");
        }
    }

    static void EnsureScopedContextOwner<TBus, TCapabilityProvider>(IServiceCollection services, string registration)
        where TBus : class, IBus
        where TCapabilityProvider : class, IScopedBusContextProvider<TBus>
    {
        Type serviceType = typeof(IScopedBusContextProvider<TBus>);
        Type defaultProvider = typeof(ScopedBusContextProvider<TBus>);
        Type capabilityProvider = typeof(TCapabilityProvider);

        ServiceDescriptor? conflicting = services.FirstOrDefault(descriptor =>
            descriptor.ServiceType == serviceType
            && descriptor.ImplementationType != defaultProvider
            && descriptor.ImplementationType != capabilityProvider);
        if (conflicting == null)
            return;

        string owner = conflicting.ImplementationType?.Name ?? conflicting.ServiceType.Name;
        throw new ConfigurationException(
            $"{registration} cannot replace the scoped bus context owner {owner} for {TypeCache<TBus>.ShortName}.");
    }

    sealed class BufferedCapacity<TBus>
        where TBus : class, IBus
    {
        public BufferedCapacity(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }
}
