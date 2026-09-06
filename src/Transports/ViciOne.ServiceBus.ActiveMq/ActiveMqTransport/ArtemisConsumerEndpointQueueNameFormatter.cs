namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Formats ActiveMQ Artemis virtual-topic subscription queue names.</summary>
public class ArtemisConsumerEndpointQueueNameFormatter :
    IActiveMqConsumerEndpointQueueNameFormatter,
    IActiveMqTopicSubscriptionNameFormatter
{
    /// <summary>Combines a consumer endpoint and topic into the ActiveMQ virtual-topic subscription convention.</summary>
    /// <param name="topic">The virtual-topic name.</param>
    /// <param name="endpointName">The consumer endpoint name.</param>
    /// <returns>A name in the form <c>Consumer.{endpoint}.{topic}</c>.</returns>
    public string Format(string topic, string endpointName)
    {
        return $"Consumer.{endpointName}.{topic}";
    }
}
