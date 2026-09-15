using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides one lazily initialized connector for each closed saga state type.</summary>
/// <typeparam name="TSaga">The saga state whose message connectors are cached.</typeparam>
public class SagaConnectorCache<TSaga> :
    ISagaConnectorCache
    where TSaga : class, ISaga
{
    readonly Lazy<SagaConnector<TSaga>> _connector;

    SagaConnectorCache()
    {
        _connector = new Lazy<SagaConnector<TSaga>>(() => new SagaConnector<TSaga>());
    }

    /// <summary>Gets the cached connector, initializing it on first access.</summary>
    public static ISagaConnector Connector => Cached.Instance.Value.Connector;

    ISagaConnector ISagaConnectorCache.Connector => _connector.Value;


    static class Cached
    {
        internal static readonly Lazy<ISagaConnectorCache> Instance = new Lazy<ISagaConnectorCache>(() => new SagaConnectorCache<TSaga>());
    }
}
