using System;

namespace ViciOne.ServiceBus.Configuration;
/// <summary>Configures the single reliable-messaging capability owned by a bus.</summary>
public interface IReliableMessagingConfigurator
{
    /// <summary>Adds a stable message contract to the one immutable application catalog.</summary>
    /// <typeparam name="TMessage">The application message type.</typeparam>
    /// <param name="name">The stable, transport-independent contract name.</param>
    /// <param name="majorVersion">The positive compatibility-breaking version.</param>
    void AddMessageContract<TMessage>(string name, int majorVersion = 1)
        where TMessage : class;

    /// <summary>Adds a message contract whose stable identity is declared by <see cref="MessageContractAttribute"/>.</summary>
    /// <typeparam name="TMessage">The attributed application message type.</typeparam>
    void AddMessageContract<TMessage>()
        where TMessage : class;

    /// <summary>Sets the hard retained-record and retained-content limits for the selected store.</summary>
    /// <param name="limits">The required positive record and logical-content bounds.</param>
    void Store(ReliableStoreLimits limits);

    /// <summary>Configures the one delivery loop, its retry policy and lease fencing.</summary>
    /// <param name="configure">The delivery-policy configuration.</param>
    void Delivery(Action<IReliableDeliveryConfigurator> configure);

    /// <summary>Sets how long terminal inbox and recurring-schedule state is retained.</summary>
    /// <param name="duration">The positive terminal-state retention duration.</param>
    void Retention(TimeSpan duration);
}

/// <summary>Configures reliable messaging for one typed bus.</summary>
/// <typeparam name="TBus">The bus whose reliable-messaging runtime is configured.</typeparam>
public interface IReliableMessagingConfigurator<TBus> : IReliableMessagingConfigurator
    where TBus : class, IBus
{
}
