namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Prepends a configured prefix to generated ActiveMQ temporary queue names.</summary>
public class PrefixTemporaryQueueNameFormatter :
    IActiveMqTemporaryQueueNameFormatter
{
    readonly string _prefix;

    /// <summary>Creates a temporary queue-name formatter.</summary>
    /// <param name="prefix">The prefix applied to every generated name.</param>
    public PrefixTemporaryQueueNameFormatter(string prefix)
    {
        _prefix = prefix;
    }

    /// <summary>Prepends the configured prefix to a generated queue name.</summary>
    /// <param name="queueName">The generated queue name.</param>
    /// <returns>The prefixed queue name.</returns>
    public string Format(string queueName)
    {
        return $"{_prefix}{queueName}";
    }
}
