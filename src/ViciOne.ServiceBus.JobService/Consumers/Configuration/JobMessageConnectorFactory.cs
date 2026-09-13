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

    /// <summary>Creates the job connector for the configured consumer and job types.</summary>
    public JobMessageConnectorFactory()
    {
        _jobConsumerConnector = new JobConsumerMessageConnector<TConsumer, TJob>();
    }

    /// <summary>Returns the connector when the requested consumer type matches this factory.</summary>
    /// <typeparam name="T">The requested consumer type.</typeparam>
    /// <returns>The typed job-consumer connector.</returns>
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
