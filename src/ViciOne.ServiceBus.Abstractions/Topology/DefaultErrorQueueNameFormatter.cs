namespace ViciOne.ServiceBus.Topology;

/// <summary>Formats default error queue name values.</summary>
public class DefaultErrorQueueNameFormatter :
    IErrorQueueNameFormatter
{
    const string ErrorQueueSuffix = "_error";

    /// <summary>Exposes the instance used by the containing type.</summary>
    public static readonly IErrorQueueNameFormatter Instance = new DefaultErrorQueueNameFormatter();

    /// <summary>Formats error queue name.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <returns>The formatted error queue name.</returns>
    public string FormatErrorQueueName(string queueName)
    {
        return queueName + ErrorQueueSuffix;
    }
}
