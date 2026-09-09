namespace ViciOne.ServiceBus.Configuration;

/// <summary>Derives the dead-letter queue name associated with a receive queue.</summary>
public interface IDeadLetterQueueNameFormatter
{
    /// <summary>Formats the dead-letter queue name for a receive queue.</summary>
    /// <param name="queueName">The source receive-queue name.</param>
    /// <returns>The associated dead-letter queue name.</returns>
    string FormatDeadLetterQueueName(string queueName);
}
