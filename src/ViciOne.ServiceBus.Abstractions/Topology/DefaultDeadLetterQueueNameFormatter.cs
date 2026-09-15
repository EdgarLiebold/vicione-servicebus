namespace ViciOne.ServiceBus.Topology;

/// <summary>Appends the standard skipped-message suffix to receive queue names.</summary>
public sealed class DefaultDeadLetterQueueNameFormatter :
    IDeadLetterQueueNameFormatter
{
    const string DeadLetterQueueSuffix = "_skipped";

    DefaultDeadLetterQueueNameFormatter()
    {
    }

    /// <summary>Gets the shared default formatter.</summary>
    public static IDeadLetterQueueNameFormatter Instance { get; } = new DefaultDeadLetterQueueNameFormatter();

    /// <summary>Creates the skipped-message queue name for a receive queue.</summary>
    /// <param name="queueName">The non-empty receive queue name.</param>
    /// <returns><paramref name="queueName" /> followed by <c>_skipped</c>.</returns>
    public string FormatDeadLetterQueueName(string queueName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        return queueName + DeadLetterQueueSuffix;
    }
}
