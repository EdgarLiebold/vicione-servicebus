using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a handler connector cache implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class HandlerConnectorCache<TMessage> :
    IHandlerConnectorCache<TMessage>
    where TMessage : class
{
    readonly HandlerConnector<TMessage> _connector;

    HandlerConnectorCache()
    {
        _connector = new HandlerConnector<TMessage>();
    }

    /// <summary>
    /// Gets the connector value.
    /// </summary>
    public static IHandlerConnector<TMessage> Connector => InstanceCache.Cached.Value.Connector;

    IHandlerConnector<TMessage> IHandlerConnectorCache<TMessage>.Connector => _connector;


    static class InstanceCache
    {
        internal static readonly Lazy<IHandlerConnectorCache<TMessage>> Cached = new Lazy<IHandlerConnectorCache<TMessage>>(
            () => new HandlerConnectorCache<TMessage>());
    }
}
