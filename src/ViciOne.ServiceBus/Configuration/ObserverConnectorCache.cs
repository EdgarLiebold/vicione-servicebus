using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides the shared consume-observer connector for a message contract.</summary>
/// <typeparam name="TMessage">The message contract observed by the shared connector.</typeparam>
public static class ObserverConnectorCache<TMessage>
    where TMessage : class
{
    static readonly Lazy<IObserverConnector<TMessage>> Cached = new(() => new MessageObserverConnector<TMessage>());

    /// <summary>Gets the lazily created process-wide connector for this message contract.</summary>
    public static IObserverConnector<TMessage> Connector => Cached.Value;
}
