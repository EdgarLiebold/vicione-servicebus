using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Configuration;

internal sealed class SagaMetadataCache<TSaga>
    where TSaga : class, ISaga
{
    readonly SagaInstanceFactoryMethod<TSaga> _factoryMethod;
    readonly SagaMessageConnectorDescriptor[] _initiatedByOrOrchestratesTypes;
    readonly SagaMessageConnectorDescriptor[] _initiatedByTypes;
    readonly SagaMessageConnectorDescriptor[] _observesTypes;
    readonly SagaMessageConnectorDescriptor[] _orchestratesTypes;

    SagaMetadataCache()
    {
        _initiatedByTypes = GetMessageContracts(typeof(IInitiatedBy<>));
        _orchestratesTypes = GetMessageContracts(typeof(IOrchestrates<>));
        _initiatedByOrOrchestratesTypes = GetMessageContracts(typeof(IInitiatedByOrOrchestrates<>));
        _observesTypes = GetMessageContracts(typeof(IObserves<,>));

        _factoryMethod = CreateSagaInstanceFactory();
    }

    public static IReadOnlyList<SagaMessageConnectorDescriptor> InitiatedByTypes => Cached.Instance.Value._initiatedByTypes;
    public static IReadOnlyList<SagaMessageConnectorDescriptor> OrchestratesTypes => Cached.Instance.Value._orchestratesTypes;
    public static IReadOnlyList<SagaMessageConnectorDescriptor> ObservesTypes => Cached.Instance.Value._observesTypes;
    public static IReadOnlyList<SagaMessageConnectorDescriptor> InitiatedByOrOrchestratesTypes =>
        Cached.Instance.Value._initiatedByOrOrchestratesTypes;
    public static SagaInstanceFactoryMethod<TSaga> FactoryMethod => Cached.Instance.Value._factoryMethod;

    static SagaInstanceFactoryMethod<TSaga> CreateSagaInstanceFactory()
    {
        if (typeof(TSaga).GetConstructor([typeof(Guid)]) is not null)
            return new ConstructorSagaInstanceFactory<TSaga>().FactoryMethod;

        if (typeof(TSaga).GetConstructor(Type.EmptyTypes) is not null
            && typeof(TSaga).GetProperty(nameof(ISaga.CorrelationId), typeof(Guid))?.SetMethod is { IsPublic: true })
        {
            return new PropertySagaInstanceFactory<TSaga>().FactoryMethod;
        }

        throw new ConfigurationException(
            global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Saga", "unknown", $"The saga {TypeCache<TSaga>.ShortName} must have either a public constructor with one Guid parameter, "
            + "or a public parameterless constructor and a writable CorrelationId property.", "Correct the named configuration before starting the host"));
    }

    static SagaMessageConnectorDescriptor[] GetMessageContracts(Type contractTypeDefinition)
    {
        return typeof(TSaga).GetInterfaces()
            .Where(x => x.IsGenericType)
            .Where(x => x.GetGenericTypeDefinition() == contractTypeDefinition)
            .Where(x => contractTypeDefinition != typeof(IObserves<,>) || x.GetGenericArguments()[1] == typeof(TSaga))
            .Select(x => x.GetGenericArguments()[0])
            .Where(MessageTypeCache.IsValidMessageType)
            .Select(x => new SagaMessageConnectorDescriptor(x, typeof(TSaga)))
            .OrderBy(x => GetStableTypeName(x.MessageType), StringComparer.Ordinal)
            .ToArray();
    }

    static string GetStableTypeName(Type type) => type.AssemblyQualifiedName ?? type.FullName ?? type.Name;


    static class Cached
    {
        internal static readonly Lazy<SagaMetadataCache<TSaga>> Instance = new Lazy<SagaMetadataCache<TSaga>>(() => new SagaMetadataCache<TSaga>());
    }
}
