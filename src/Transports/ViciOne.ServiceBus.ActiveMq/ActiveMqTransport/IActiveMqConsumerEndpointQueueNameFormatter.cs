namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for active mq consumer endpoint queue name formatter.
/// </summary>
public interface IActiveMqConsumerEndpointQueueNameFormatter
{
    /// <summary>
    /// Performs the format operation.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="endpointName">The endpoint name value.</param>
    /// <returns>The result of the operation.</returns>
    public string Format(string topic, string endpointName);
}
