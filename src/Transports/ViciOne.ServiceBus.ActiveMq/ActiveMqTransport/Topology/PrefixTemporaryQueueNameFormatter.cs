namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>
/// Provides a prefix temporary queue name formatter implementation.
/// </summary>
public class PrefixTemporaryQueueNameFormatter :
    IActiveMqTemporaryQueueNameFormatter
{
    readonly string _prefix;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="prefix">The prefix value.</param>
    public PrefixTemporaryQueueNameFormatter(string prefix)
    {
        _prefix = prefix;
    }

    /// <summary>
    /// Performs the format operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
    public string Format(string queueName)
    {
        return $"{_prefix}{queueName}";
    }
}
