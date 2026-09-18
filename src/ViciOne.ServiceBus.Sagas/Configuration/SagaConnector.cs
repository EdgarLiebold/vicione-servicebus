using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Discovers a saga's supported message contracts and connects their repository pipelines.</summary>
/// <typeparam name="TSaga">The saga state implementing supported saga message contracts.</typeparam>
public sealed class SagaConnector<TSaga> :
    ISagaConnector
    where TSaga : class, ISaga
{
    readonly List<ISagaMessageConnector<TSaga>> _connectors;
    readonly IEnumerable<ISagaMessageConnector> _connectorView;

    /// <summary>Discovers message connectors in category precedence order and rejects a saga with no supported contracts.</summary>
    public SagaConnector()
    {
        _connectors = Initiates()
            .Concat(Orchestrates())
            .Concat(InitiatesOrOrchestrates())
            .Concat(Observes())
            // Category order is semantic: initiation owns a duplicated message contract
            // before orchestration, combined initiation/orchestration, and observation.
            // Ordering inside a category is stable and independent of reflection order.
            .DistinctBy(x => x.MessageType)
            .ToList();

        if (_connectors.Count == 0)
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Saga", "unknown", $"The saga {TypeCache<TSaga>.ShortName} does not declare a supported saga message contract.", "Correct the named configuration before starting the host"));
        }

        _connectorView = _connectors.AsReadOnly();
    }

    /// <summary>Gets a read-only view containing one connector per message contract, in category precedence order.</summary>
    public IEnumerable<ISagaMessageConnector> Connectors => _connectorView;

    ISagaSpecification<T> ISagaConnector.CreateSagaSpecification<T>()
    {
        if (typeof(T) != typeof(TSaga))
            throw new ArgumentException("The generic argument did not match the connector type", nameof(T));

        var specification = new SagaSpecification<TSaga>(
            _connectors.Select(x => x.CreateSagaMessageSpecification()).ToList());

        return (ISagaSpecification<T>)(object)specification;
    }

    ConnectHandle ISagaConnector.ConnectSaga<T>(IConsumePipeConnector consumePipe, ISagaRepository<T> repository, ISagaSpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(consumePipe);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(specification);

        if (typeof(T) != typeof(TSaga))
            throw new ArgumentException("The generic argument did not match the connector type", nameof(T));

        var handles = new List<ConnectHandle>(_connectors.Count);
        try
        {
            foreach (ISagaMessageConnector<TSaga> connector in _connectors)
            {
                var handle = connector.ConnectSaga(
                        consumePipe,
                        (ISagaRepository<TSaga>)(object)repository,
                        (ISagaSpecification<TSaga>)(object)specification)
                    ?? throw new InvalidOperationException(
                        $"The saga message connector for {TypeCache.GetShortName(connector.MessageType)} returned no connection handle.");

                handles.Add(handle);
            }

            return new MultipleConnectHandle(handles);
        }
        catch (Exception connectionFailure)
        {
            List<Exception>? cleanupFailures = null;
            foreach (var handle in handles)
            {
                try
                {
                    handle.Dispose();
                }
                catch (Exception cleanupFailure)
                {
                    cleanupFailures ??= [];
                    cleanupFailures.Add(cleanupFailure);
                }
            }

            if (cleanupFailures is null)
                throw;

            var failures = new List<Exception>(cleanupFailures.Count + 1) { connectionFailure };
            failures.AddRange(cleanupFailures);
            throw new AggregateException("Saga connection and cleanup both failed.", failures);
        }
    }

    static IEnumerable<ISagaMessageConnector<TSaga>> Initiates()
    {
        return SagaMetadataCache<TSaga>.InitiatedByTypes.Select(x => x.CreateInitiatedByConnector<TSaga>());
    }

    static IEnumerable<ISagaMessageConnector<TSaga>> Orchestrates()
    {
        return SagaMetadataCache<TSaga>.OrchestratesTypes.Select(x => x.CreateOrchestratesConnector<TSaga>());
    }

    static IEnumerable<ISagaMessageConnector<TSaga>> Observes()
    {
        return SagaMetadataCache<TSaga>.ObservesTypes.Select(x => x.CreateObservesConnector<TSaga>());
    }

    static IEnumerable<ISagaMessageConnector<TSaga>> InitiatesOrOrchestrates()
    {
        return SagaMetadataCache<TSaga>.InitiatedByOrOrchestratesTypes.Select(x => x.CreateInitiatedByOrOrchestratesConnector<TSaga>());
    }
}
