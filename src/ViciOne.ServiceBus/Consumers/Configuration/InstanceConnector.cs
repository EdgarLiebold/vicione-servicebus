using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an instance connector implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
public class InstanceConnector<TConsumer> :
    IInstanceConnector
    where TConsumer : class
{
    readonly List<IInstanceMessageConnector<TConsumer>> _connectors;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public InstanceConnector()
    {
        if (RegistrationMetadata.IsConsumerRegistrationExcluded(typeof(TConsumer)))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Instance Connector", "unknown", "A saga cannot be registered as a consumer", "Correct the named configuration before starting the host"));

        _connectors = Consumes()
            .ToList();
    }

    /// <summary>
    /// Connects instance.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="pipeConnector">The pipe connector value.</param>
    /// <param name="instance">The instance value.</param>
    /// <param name="specification">The specification value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectInstance<T>(IConsumePipeConnector pipeConnector, T instance, IConsumerSpecification<T> specification)
        where T : class
    {
        var handles = new List<ConnectHandle>(_connectors.Count);
        try
        {
            foreach (IInstanceMessageConnector<T> connector in _connectors.Cast<IInstanceMessageConnector<T>>())
            {
                var handle = connector.ConnectInstance(pipeConnector, instance, specification);

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

    /// <summary>
    /// Connects instance.
    /// </summary>
    /// <param name="pipeConnector">The pipe connector value.</param>
    /// <param name="instance">The instance value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectInstance(IConsumePipeConnector pipeConnector, object instance)
    {
        if (instance is TConsumer consumer)
        {
            IConsumerSpecification<TConsumer> specification = CreateConsumerSpecification<TConsumer>();

            return ConnectInstance(pipeConnector, consumer, specification);
        }

        throw new ConsumerException(
            $"The instance type {TypeCache.GetShortName(instance.GetType())} does not match the consumer type: {TypeCache<TConsumer>.ShortName}");
    }

    /// <summary>
    /// Creates consumer specification.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public IConsumerSpecification<T> CreateConsumerSpecification<T>()
        where T : class
    {
        List<IConsumerMessageSpecification<T>> messageSpecifications =
            _connectors.Select(x => x.CreateConsumerMessageSpecification())
                .Cast<IConsumerMessageSpecification<T>>()
                .ToList();

        return new ConsumerSpecification<T>(messageSpecifications);
    }

    static IEnumerable<IInstanceMessageConnector<TConsumer>> Consumes()
    {
        return ConsumerMetadataCache<TConsumer>.ConsumerTypes.Select(x => x.GetInstanceConnector<TConsumer>());
    }
}
