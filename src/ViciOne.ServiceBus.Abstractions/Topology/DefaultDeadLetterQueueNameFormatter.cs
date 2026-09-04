namespace ViciOne.ServiceBus.Topology;

/// <summary>
/// Provides a default dead letter queue name formatter implementation.
/// </summary>
public class DefaultDeadLetterQueueNameFormatter :
    IDeadLetterQueueNameFormatter
{
    const string DeadLetterQueueSuffix = "_skipped";

    /// <summary>
    /// Defines the instance value.
    /// </summary>
    public static readonly IDeadLetterQueueNameFormatter Instance = new DefaultDeadLetterQueueNameFormatter();

    /// <summary>
    /// Performs the format dead letter queue name operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
    public string FormatDeadLetterQueueName(string queueName)
    {
        return queueName + DeadLetterQueueSuffix;
    }
}
