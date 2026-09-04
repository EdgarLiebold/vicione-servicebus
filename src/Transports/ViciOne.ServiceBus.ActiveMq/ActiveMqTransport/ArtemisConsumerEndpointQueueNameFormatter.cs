namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Provides an artemis consumer endpoint queue name formatter implementation.
/// </summary>
public class ArtemisConsumerEndpointQueueNameFormatter :
    IActiveMqConsumerEndpointQueueNameFormatter,
    IActiveMqTopicSubscriptionNameFormatter
{
    /// <summary>
    /// Performs the format operation.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="endpointName">The endpoint name value.</param>
    /// <returns>The result of the operation.</returns>
    public string Format(string topic, string endpointName)
    {
        return $"Consumer.{endpointName}.{topic}";
    }
}
