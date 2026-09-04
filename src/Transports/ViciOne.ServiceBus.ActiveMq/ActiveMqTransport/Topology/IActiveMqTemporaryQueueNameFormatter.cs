namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>
/// Defines the contract for active mq temporary queue name formatter.
/// </summary>
public interface IActiveMqTemporaryQueueNameFormatter
{
    /// <summary>
    /// Performs the format operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
    public string Format(string queueName);
}
