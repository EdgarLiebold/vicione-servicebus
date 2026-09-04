using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a job message connector factory implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
/// <typeparam name="TJob">The t job type.</typeparam>
public class JobMessageConnectorFactory<TConsumer, TJob> :
    IMessageConnectorFactory
    where TConsumer : class, IJobConsumer<TJob>
    where TJob : class
{
    readonly IConsumerMessageConnector<TConsumer> _jobConsumerConnector;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public JobMessageConnectorFactory()
    {
        _jobConsumerConnector = new JobConsumerMessageConnector<TConsumer, TJob>();
    }

    /// <summary>
    /// Creates consumer connector.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
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
