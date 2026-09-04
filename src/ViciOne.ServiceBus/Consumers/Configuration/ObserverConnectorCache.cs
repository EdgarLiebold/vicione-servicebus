using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an observer connector cache implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class ObserverConnectorCache<TMessage> :
    IObserverConnectorCache<TMessage>
    where TMessage : class
{
    readonly Lazy<MessageObserverConnector<TMessage>> _connector;

    ObserverConnectorCache()
    {
        _connector = new Lazy<MessageObserverConnector<TMessage>>(() => new MessageObserverConnector<TMessage>());
    }

    /// <summary>
    /// Gets the connector value.
    /// </summary>
    public static IObserverConnector<TMessage> Connector => InstanceCache.Cached.Value.Connector;

    IObserverConnector<TMessage> IObserverConnectorCache<TMessage>.Connector => _connector.Value;


    static class InstanceCache
    {
        internal static readonly Lazy<IObserverConnectorCache<TMessage>> Cached =
            new Lazy<IObserverConnectorCache<TMessage>>(() => new ObserverConnectorCache<TMessage>());
    }
}
