using System;

#nullable enable

namespace ViciOne.ServiceBus.Configuration;
/// <summary>Configures the single reliable-messaging capability owned by a bus.</summary>
public interface IReliableMessagingConfigurator
{
    /// <summary>Adds a stable message contract to the one immutable application catalog.</summary>
    void AddMessageContract<TMessage>(string name, int majorVersion = 1)
        where TMessage : class;

    /// <summary>Adds a message contract whose stable identity is declared by <see cref="MessageContractAttribute"/>.</summary>
    void AddMessageContract<TMessage>()
        where TMessage : class;

    /// <summary>Sets the hard retained-record and retained-content limits for the selected store.</summary>
    void Store(ReliableStoreLimits limits);

    /// <summary>Configures the one delivery loop, its retry policy and lease fencing.</summary>
    void Delivery(Action<IReliableDeliveryConfigurator> configure);

    /// <summary>Sets how long terminal inbox and recurring-schedule state is retained.</summary>
    void Retention(TimeSpan duration);
}

/// <summary>Configures reliable messaging for one typed bus.</summary>
public interface IReliableMessagingConfigurator<TBus> : IReliableMessagingConfigurator
    where TBus : class, IBus
{
}
