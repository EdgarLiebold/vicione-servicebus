using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a consumer connector implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class ConsumerConnector<T> :
    IConsumerConnector
    where T : class
{
    readonly List<IConsumerMessageConnector<T>> _connectors;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ConsumerConnector()
    {
        if (RegistrationMetadata.IsSaga(typeof(T)))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Consumer Connector", "unknown", "A saga cannot be registered as a consumer", "Correct the named configuration before starting the host"));

        _connectors = Consumes().ToList();
    }

    /// <summary>
    /// Gets the connectors value.
    /// </summary>
    public IEnumerable<IConsumerMessageConnector> Connectors => _connectors;

    ConnectHandle IConsumerConnector.ConnectConsumer<TConsumer>(IConsumePipeConnector consumePipe, IConsumerFactory<TConsumer> consumerFactory,
        IConsumerSpecification<TConsumer> specification)
    {
        var handles = new List<ConnectHandle>(_connectors.Count);
        try
        {
            foreach (IConsumerMessageConnector<TConsumer> connector in _connectors.Cast<IConsumerMessageConnector<TConsumer>>())
            {
                var handle = connector.ConnectConsumer(consumePipe, consumerFactory, specification);

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

    IConsumerSpecification<TConsumer> IConsumerConnector.CreateConsumerSpecification<TConsumer>()
    {
        List<IConsumerMessageSpecification<TConsumer>> messageSpecifications =
            _connectors.Select(x => x.CreateConsumerMessageSpecification())
                .Cast<IConsumerMessageSpecification<TConsumer>>()
                .ToList();

        return new ConsumerSpecification<TConsumer>(messageSpecifications);
    }

    static IEnumerable<IConsumerMessageConnector<T>> Consumes()
    {
        return ConsumerMetadataCache<T>.ConsumerTypes.Select(x => x.GetConsumerConnector<T>());
    }
}
