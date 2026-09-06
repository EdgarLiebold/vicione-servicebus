namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Formats consumer queue or subscription names for ActiveMQ virtual topics.</summary>
public interface IActiveMqConsumerEndpointQueueNameFormatter
{
    /// <summary>Combines a topic and receive endpoint into a broker consumer name.</summary>
    /// <param name="topic">The virtual-topic name.</param>
    /// <param name="endpointName">The receive-endpoint name.</param>
    /// <returns>The queue or subscription name.</returns>
    public string Format(string topic, string endpointName);
}
