namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for error queue name formatter.
/// </summary>
public interface IErrorQueueNameFormatter
{
    /// <summary>
    /// Performs the format error queue name operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
    string FormatErrorQueueName(string queueName);
}
