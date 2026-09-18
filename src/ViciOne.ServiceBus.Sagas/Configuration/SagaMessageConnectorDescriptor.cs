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
    readonly Type _sagaType;

    public SagaMessageConnectorDescriptor(Type messageType, Type sagaType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(sagaType);
        if (!MessageTypeCache.IsValidMessageType(messageType))
        {
            throw new ArgumentException(
                MessageTypeCache.InvalidMessageTypeReason(messageType) ?? "The message type is not a valid message contract.",
                nameof(messageType));
        }

        if (sagaType.IsValueType || sagaType.ContainsGenericParameters || !typeof(ISaga).IsAssignableFrom(sagaType))
        {
            throw new ArgumentException(
                "The saga type must be a closed reference type implementing ISaga.",
                nameof(sagaType));
        }

        MessageType = messageType;
        _sagaType = sagaType;

        _initiatedByFactory = CreateFactory(typeof(InitiatedBySagaConnectorFactory<,>), sagaType, messageType);
        _orchestratesFactory = CreateFactory(typeof(OrchestratesSagaConnectorFactory<,>), sagaType, messageType);
        _initiatedByOrOrchestratesFactory = CreateFactory(typeof(InitiatedByOrOrchestratesSagaConnectorFactory<,>), sagaType, messageType);
        _observesFactory = CreateFactory(typeof(ObservesSagaConnectorFactory<,>), sagaType, messageType);
    }

    public Type MessageType { get; }

    public ISagaMessageConnector<TSaga> CreateInitiatedByConnector<TSaga>()
        where TSaga : class, ISaga
    {
        ValidateSagaType<TSaga>();
        return _initiatedByFactory.Value.CreateMessageConnector<TSaga>();
    }

    public ISagaMessageConnector<TSaga> CreateOrchestratesConnector<TSaga>()
        where TSaga : class, ISaga
    {
        ValidateSagaType<TSaga>();
        return _orchestratesFactory.Value.CreateMessageConnector<TSaga>();
    }

    public ISagaMessageConnector<TSaga> CreateObservesConnector<TSaga>()
        where TSaga : class, ISaga
    {
        ValidateSagaType<TSaga>();
        return _observesFactory.Value.CreateMessageConnector<TSaga>();
    }

    public ISagaMessageConnector<TSaga> CreateInitiatedByOrOrchestratesConnector<TSaga>()
        where TSaga : class, ISaga
    {
        ValidateSagaType<TSaga>();
        return _initiatedByOrOrchestratesFactory.Value.CreateMessageConnector<TSaga>();
    }

    void ValidateSagaType<TSaga>()
        where TSaga : class, ISaga
    {
        if (typeof(TSaga) != _sagaType)
        {
            throw new ArgumentException(
                $"The requested saga type {TypeCache<TSaga>.ShortName} does not match the descriptor saga type {TypeCache.GetShortName(_sagaType)}.",
                nameof(TSaga));
        }
    }

    static Lazy<ISagaConnectorFactory> CreateFactory(Type openFactoryType, Type sagaType, Type messageType)
    {
        return new Lazy<ISagaConnectorFactory>(() =>
        {
            try
            {
                Type factoryType = openFactoryType.MakeGenericType(sagaType, messageType);
                return (ISagaConnectorFactory)Activator.CreateInstance(factoryType)!;
            }
            catch (Exception exception)
            {
                string factoryName = openFactoryType.Name.Split('`')[0];
                throw new ConfigurationException(
                    global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                        "Saga",
                        TypeCache.GetShortName(sagaType),
                        $"Unable to create {factoryName} for message {TypeCache.GetShortName(messageType)}.",
                        "Correct the named configuration before starting the host"),
                    exception);
            }
        });
    }
}
