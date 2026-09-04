namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for dead letter queue name formatter.
/// </summary>
public interface IDeadLetterQueueNameFormatter
{
    /// <summary>
    /// Performs the format dead letter queue name operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
    string FormatDeadLetterQueueName(string queueName);
}
