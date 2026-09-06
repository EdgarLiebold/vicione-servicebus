namespace ViciOne.ServiceBus.Topology;

/// <summary>Formats default dead letter queue name values.</summary>
public class DefaultDeadLetterQueueNameFormatter :
    IDeadLetterQueueNameFormatter
{
    const string DeadLetterQueueSuffix = "_skipped";

    /// <summary>Exposes the instance used by the containing type.</summary>
    public static readonly IDeadLetterQueueNameFormatter Instance = new DefaultDeadLetterQueueNameFormatter();

    /// <summary>Formats dead letter queue name.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <returns>The formatted dead letter queue name.</returns>
    public string FormatDeadLetterQueueName(string queueName)
    {
        return queueName + DeadLetterQueueSuffix;
    }
}
