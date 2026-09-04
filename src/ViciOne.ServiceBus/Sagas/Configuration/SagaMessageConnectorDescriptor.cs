using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Describes one saga message contract and lazily creates the connector for each supported
/// saga role. The descriptor is an implementation detail of saga discovery, not public API.
/// </summary>
internal sealed class SagaMessageConnectorDescriptor
{
    readonly Lazy<ISagaConnectorFactory> _initiatedByFactory;
    readonly Lazy<ISagaConnectorFactory> _initiatedByOrOrchestratesFactory;
    readonly Lazy<ISagaConnectorFactory> _observesFactory;
    readonly Lazy<ISagaConnectorFactory> _orchestratesFactory;

    public SagaMessageConnectorDescriptor(Type messageType, Type sagaType)
    {
        MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        ArgumentNullException.ThrowIfNull(sagaType);

        _initiatedByFactory = CreateFactory(typeof(InitiatedBySagaConnectorFactory<,>), sagaType, messageType);
        _orchestratesFactory = CreateFactory(typeof(OrchestratesSagaConnectorFactory<,>), sagaType, messageType);
        _initiatedByOrOrchestratesFactory = CreateFactory(typeof(InitiatedByOrOrchestratesSagaConnectorFactory<,>), sagaType, messageType);
        _observesFactory = CreateFactory(typeof(ObservesSagaConnectorFactory<,>), sagaType, messageType);
    }

    public Type MessageType { get; }

    public ISagaMessageConnector<TSaga> CreateInitiatedByConnector<TSaga>()
        where TSaga : class, ISaga
    {
        return _initiatedByFactory.Value.CreateMessageConnector<TSaga>();
    }

    public ISagaMessageConnector<TSaga> CreateOrchestratesConnector<TSaga>()
        where TSaga : class, ISaga
    {
        return _orchestratesFactory.Value.CreateMessageConnector<TSaga>();
    }

    public ISagaMessageConnector<TSaga> CreateObservesConnector<TSaga>()
        where TSaga : class, ISaga
    {
        return _observesFactory.Value.CreateMessageConnector<TSaga>();
    }

    public ISagaMessageConnector<TSaga> CreateInitiatedByOrOrchestratesConnector<TSaga>()
        where TSaga : class, ISaga
    {
        return _initiatedByOrOrchestratesFactory.Value.CreateMessageConnector<TSaga>();
    }

    static Lazy<ISagaConnectorFactory> CreateFactory(Type openFactoryType, Type sagaType, Type messageType)
    {
        return new Lazy<ISagaConnectorFactory>(() =>
        {
            Type factoryType = openFactoryType.MakeGenericType(sagaType, messageType);

            return (ISagaConnectorFactory)(Activator.CreateInstance(factoryType)
                ?? throw new ConfigurationException($"Unable to create saga connector factory {TypeCache.GetShortName(factoryType)}."));
        });
    }
}
