using System;

#nullable enable

namespace ViciOne.ServiceBus.Configuration;
/// <summary>High-level configuration shared by producer-side durable senders.</summary>
public interface IDurableSenderConfigurator
{
    /// <summary>Adds a stable message contract to the one immutable application catalog.</summary>
    void AddMessageContract<TMessage>(string name, int majorVersion = 1)
        where TMessage : class;

    /// <summary>Adds a message contract whose stable identity is declared by <see cref="MessageContractAttribute"/>.</summary>
    void AddMessageContract<TMessage>()
        where TMessage : class;
}

/// <summary>High-level configuration for producer-side durable sending on one typed bus.</summary>
public interface IDurableSenderConfigurator<TBus> : IDurableSenderConfigurator
    where TBus : class, IBus
{
    /// <summary>Configures bounded storage, delivery, retry and health policies.</summary>
    void Configure(Action<DurableSenderOptions<TBus>> configure);
}
