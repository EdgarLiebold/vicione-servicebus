namespace ViciOne.ServiceBus.Topology;

/// <summary>
/// Provides a default error queue name formatter implementation.
/// </summary>
public class DefaultErrorQueueNameFormatter :
    IErrorQueueNameFormatter
{
    const string ErrorQueueSuffix = "_error";

    /// <summary>
    /// Defines the instance value.
    /// </summary>
    public static readonly IErrorQueueNameFormatter Instance = new DefaultErrorQueueNameFormatter();

    /// <summary>
    /// Performs the format error queue name operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
    public string FormatErrorQueueName(string queueName)
    {
        return queueName + ErrorQueueSuffix;
    }
}
