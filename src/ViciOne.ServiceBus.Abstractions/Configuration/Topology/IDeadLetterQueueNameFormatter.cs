namespace ViciOne.ServiceBus.Configuration;

/// <summary>Formats dead letter queue name values.</summary>
public interface IDeadLetterQueueNameFormatter
{
    /// <summary>Formats dead letter queue name.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <returns>The formatted dead letter queue name.</returns>
    string FormatDeadLetterQueueName(string queueName);
}
