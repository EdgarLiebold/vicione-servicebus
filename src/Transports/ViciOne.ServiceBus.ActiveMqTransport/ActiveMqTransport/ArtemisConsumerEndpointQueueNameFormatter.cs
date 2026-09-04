namespace ViciOne.ServiceBus.ActiveMqTransport;

public class ArtemisConsumerEndpointQueueNameFormatter :
    IActiveMqConsumerEndpointQueueNameFormatter,
    IActiveMqTopicSubscriptionNameFormatter
{
    public string Format(string topic, string endpointName)
    {
        return $"Consumer.{endpointName}.{topic}";
    }
}
