namespace ViciOne.ServiceBus.Configuration;

/// <summary>Derives the error queue name associated with a receive queue.</summary>
public interface IErrorQueueNameFormatter
{
    /// <summary>Formats the error queue name for a receive queue.</summary>
    /// <param name="queueName">The source receive-queue name.</param>
    /// <returns>The associated error queue name.</returns>
    string FormatErrorQueueName(string queueName);
}
