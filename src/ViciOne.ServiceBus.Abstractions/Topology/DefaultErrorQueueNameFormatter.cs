namespace ViciOne.ServiceBus.Topology;

/// <summary>Appends the standard error suffix to receive queue names.</summary>
public sealed class DefaultErrorQueueNameFormatter :
    IErrorQueueNameFormatter
{
    const string ErrorQueueSuffix = "_error";

    DefaultErrorQueueNameFormatter()
    {
    }

    /// <summary>Gets the shared default formatter.</summary>
    public static IErrorQueueNameFormatter Instance { get; } = new DefaultErrorQueueNameFormatter();

    /// <summary>Creates the error queue name for a receive queue.</summary>
    /// <param name="queueName">The non-empty receive queue name.</param>
    /// <returns><paramref name="queueName" /> followed by <c>_error</c>.</returns>
    public string FormatErrorQueueName(string queueName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        return queueName + ErrorQueueSuffix;
    }
}
