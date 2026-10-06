using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Consumers.Metadata;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects every discovered message contract for an existing consumer instance.</summary>
/// <typeparam name="TConsumer">The consumer implementation whose existing instances are connected.</typeparam>
public sealed class InstanceConnector<TConsumer> :
    IInstanceConnector
    where TConsumer : class
{
    readonly List<IInstanceMessageConnector<TConsumer>> _connectors;

    /// <summary>Discovers and materializes instance connectors for every convention-recognized consumer contract.</summary>
    public InstanceConnector()
    {
        if (ConsumerRegistrationMetadata.IsConsumerRegistrationExcluded(typeof(TConsumer)))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Instance Connector", "unknown", "A saga cannot be registered as a consumer", "Correct the named configuration before starting the host"));

        _connectors = Consumes()
            .ToList();
        if (_connectors.Count == 0)
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Instance Connector",
                    "unknown",
                    $"No registered consumer convention discovered a message contract on '{TypeCache<TConsumer>.ShortName}'",
                    "Register a matching consumer convention or implement a supported consumer contract"));
        }
    }

    /// <summary>Connects every discovered message contract for a strongly typed consumer instance.</summary>
    /// <typeparam name="TRequestedConsumer">The consumer type requested by the caller.</typeparam>
    /// <param name="pipeConnector">The consume pipe that will dispatch matching messages.</param>
    /// <param name="instance">The consumer instance to invoke.</param>
    /// <param name="specification">The consumer and message pipeline configuration to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectInstance<TRequestedConsumer>(IConsumePipeConnector pipeConnector, TRequestedConsumer instance,
        IConsumerSpecification<TRequestedConsumer> specification)
        where TRequestedConsumer : class
    {
        ArgumentNullException.ThrowIfNull(pipeConnector);
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(specification);

        var handles = new List<ConnectHandle>(_connectors.Count);
        try
        {
            foreach (IInstanceMessageConnector<TRequestedConsumer> connector in _connectors.Cast<IInstanceMessageConnector<TRequestedConsumer>>())
            {
                var handle = connector.ConnectInstance(pipeConnector, instance, specification);

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
                throw new AggregateException("Instance admission and registration cleanup failed.", cleanupFailures);
            }

            throw;
        }
    }

    /// <summary>Connects every discovered consumer contract of a runtime object instance.</summary>
    /// <param name="pipeConnector">The consume pipe that will dispatch matching messages.</param>
    /// <param name="instance">The consumer instance to invoke.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectInstance(IConsumePipeConnector pipeConnector, object instance)
    {
        ArgumentNullException.ThrowIfNull(pipeConnector);
        ArgumentNullException.ThrowIfNull(instance);

        if (instance is TConsumer consumer)
        {
            IConsumerSpecification<TConsumer> specification = CreateConsumerSpecification<TConsumer>();

            return ConnectInstance(pipeConnector, consumer, specification);
        }

        throw new ConsumerException(
            $"The instance type {TypeCache.GetShortName(instance.GetType())} does not match the consumer type: {TypeCache<TConsumer>.ShortName}");
    }

    /// <summary>Creates consumer specification.</summary>
    /// <typeparam name="TRequestedConsumer">The consumer type requested by the caller.</typeparam>
    /// <returns>The created consumer specification.</returns>
    public IConsumerSpecification<TRequestedConsumer> CreateConsumerSpecification<TRequestedConsumer>()
        where TRequestedConsumer : class
    {
        List<IConsumerMessageSpecification<TRequestedConsumer>> messageSpecifications =
            _connectors.Select(x => x.CreateConsumerMessageSpecification())
                .Cast<IConsumerMessageSpecification<TRequestedConsumer>>()
                .ToList();

        return new ConsumerSpecification<TRequestedConsumer>(messageSpecifications);
    }

    static IEnumerable<IInstanceMessageConnector<TConsumer>> Consumes()
    {
        return ConsumerMetadataCache<TConsumer>.ConsumerTypes.Select(x => x.GetInstanceConnector<TConsumer>());
    }
}
