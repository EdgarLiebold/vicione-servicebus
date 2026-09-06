using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Caches handler connector data.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class HandlerConnectorCache<TMessage> :
    IHandlerConnectorCache<TMessage>
    where TMessage : class
{
    readonly HandlerConnector<TMessage> _connector;

    HandlerConnectorCache()
    {
        _connector = new HandlerConnector<TMessage>();
    }

    /// <summary>Gets the connector.</summary>
    public static IHandlerConnector<TMessage> Connector => InstanceCache.Cached.Value.Connector;

    IHandlerConnector<TMessage> IHandlerConnectorCache<TMessage>.Connector => _connector;


    static class InstanceCache
    {
        internal static readonly Lazy<IHandlerConnectorCache<TMessage>> Cached = new Lazy<IHandlerConnectorCache<TMessage>>(
            () => new HandlerConnectorCache<TMessage>());
    }
}
