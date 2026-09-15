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

    /// <summary>Discovers message connectors in category precedence order and rejects a saga with no supported contracts.</summary>
    public SagaConnector()
    {
        try
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
        }
        catch (ConfigurationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Saga", "unknown", $"Failed to create the saga connector for {TypeCache<TSaga>.ShortName}.", "Correct the named configuration before starting the host"), ex);
        }
    }

    /// <summary>Gets one connector per message contract, in initiation, orchestration, combined and observation category order.</summary>
    public IEnumerable<ISagaMessageConnector> Connectors => _connectors;

    ISagaSpecification<T> ISagaConnector.CreateSagaSpecification<T>()
    {
        return new SagaSpecification<T>(_connectors.Select(x => x.CreateSagaMessageSpecification())
            .Cast<ISagaMessageSpecification<T>>()
            .ToList());
    }

    ConnectHandle ISagaConnector.ConnectSaga<T>(IConsumePipeConnector consumePipe, ISagaRepository<T> repository, ISagaSpecification<T> specification)
    {
        var handles = new List<ConnectHandle>(_connectors.Count);
        try
        {
            foreach (ISagaMessageConnector<T> connector in _connectors.Cast<ISagaMessageConnector<T>>())
            {
                var handle = connector.ConnectSaga(consumePipe, repository, specification);

                handles.Add(handle);
            }

            return new MultipleConnectHandle(handles);
        }
        catch (Exception)
        {
            foreach (var handle in handles)
                handle.Dispose();
            throw;
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
