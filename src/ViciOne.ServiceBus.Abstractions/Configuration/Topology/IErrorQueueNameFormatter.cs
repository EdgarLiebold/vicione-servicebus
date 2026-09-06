namespace ViciOne.ServiceBus.Configuration;

/// <summary>Formats error queue name values.</summary>
public interface IErrorQueueNameFormatter
{
    /// <summary>Formats error queue name.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <returns>The formatted error queue name.</returns>
    string FormatErrorQueueName(string queueName);
}
