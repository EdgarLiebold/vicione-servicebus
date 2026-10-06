using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Consumers.Metadata;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects every discovered message contract for a consumer to a consume pipe.</summary>
/// <typeparam name="TConsumer">The consumer type.</typeparam>
public sealed class ConsumerConnector<TConsumer> :
    IConsumerConnector
    where TConsumer : class
{
    readonly IConsumerMessageConnector<TConsumer>[] _connectors;

    /// <summary>Discovers and materializes connectors for every convention-recognized consumer contract.</summary>
    public ConsumerConnector()
    {
        if (ConsumerRegistrationMetadata.IsConsumerRegistrationExcluded(typeof(TConsumer)))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Consumer Connector", "unknown", "A saga cannot be registered as a consumer", "Correct the named configuration before starting the host"));

        _connectors = Consumes().ToArray();
        if (_connectors.Length == 0)
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Consumer Connector",
                    "unknown",
                    $"No registered consumer convention discovered a message contract on '{TypeCache<TConsumer>.ShortName}'",
                    "Register a matching consumer convention or implement a supported consumer contract"));
        }
    }

    /// <summary>Gets a read-only view of the discovered message connectors.</summary>
    public IReadOnlyList<IConsumerMessageConnector> Connectors => _connectors;

    ConnectHandle IConsumerConnector.ConnectConsumer<TRequestedConsumer>(IConsumePipeConnector consumePipe,
        IConsumerFactory<TRequestedConsumer> consumerFactory, IConsumerSpecification<TRequestedConsumer> specification)
    {
        ArgumentNullException.ThrowIfNull(consumePipe);
        ArgumentNullException.ThrowIfNull(consumerFactory);
        ArgumentNullException.ThrowIfNull(specification);

        var handles = new List<ConnectHandle>(_connectors.Length);
        try
        {
            foreach (IConsumerMessageConnector<TRequestedConsumer> connector in _connectors.Cast<IConsumerMessageConnector<TRequestedConsumer>>())
            {
                var handle = connector.ConnectConsumer(consumePipe, consumerFactory, specification);

                handles.Add(handle);
            }

            return new MultipleConnectHandle(handles);
        }
        catch (Exception admissionFailure)
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
                    (cleanupFailures ??= []).Add(cleanupFailure);
                }
            }

            if (cleanupFailures is not null)
            {
                cleanupFailures.Insert(0, admissionFailure);
                throw new AggregateException("Consumer admission and registration cleanup failed.", cleanupFailures);
            }

            throw;
        }
    }

    IConsumerSpecification<TRequestedConsumer> IConsumerConnector.CreateConsumerSpecification<TRequestedConsumer>()
    {
        List<IConsumerMessageSpecification<TRequestedConsumer>> messageSpecifications =
            _connectors.Select(x => x.CreateConsumerMessageSpecification())
                .Cast<IConsumerMessageSpecification<TRequestedConsumer>>()
                .ToList();

        return new ConsumerSpecification<TRequestedConsumer>(messageSpecifications);
    }

    static IEnumerable<IConsumerMessageConnector<TConsumer>> Consumes()
    {
        return ConsumerMetadataCache<TConsumer>.ConsumerTypes.Select(x => x.GetConsumerConnector<TConsumer>());
    }
}
