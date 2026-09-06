using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates job message connector instances.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TJob">The job type.</typeparam>
internal sealed class JobMessageConnectorFactory<TConsumer, TJob> :
    IMessageConnectorFactory
    where TConsumer : class, IJobConsumer<TJob>
    where TJob : class
{
    readonly IConsumerMessageConnector<TConsumer> _jobConsumerConnector;

    /// <summary>Initializes a new instance.</summary>
    public JobMessageConnectorFactory()
    {
        _jobConsumerConnector = new JobConsumerMessageConnector<TConsumer, TJob>();
    }

    /// <summary>Creates consumer connector.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The created consumer connector.</returns>
    public IConsumerMessageConnector<T> CreateConsumerConnector<T>()
        where T : class
    {
        return _jobConsumerConnector as IConsumerMessageConnector<T> ?? throw new ArgumentException("The consumer type did not match the connector type");
    }

    IInstanceMessageConnector<T> IMessageConnectorFactory.CreateInstanceConnector<T>()
    {
        throw new NotSupportedException($"{TypeCache<TJob>.ShortName} jobs cannot be connected to consumer instances.");
    }
}
