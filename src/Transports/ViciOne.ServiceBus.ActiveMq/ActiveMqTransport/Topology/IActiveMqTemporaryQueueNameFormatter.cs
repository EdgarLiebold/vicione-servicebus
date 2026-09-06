namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Formats generated ActiveMQ temporary queue names.</summary>
public interface IActiveMqTemporaryQueueNameFormatter
{
    /// <summary>Transforms a generated temporary queue name.</summary>
    /// <param name="queueName">The generated queue name.</param>
    /// <returns>The broker queue name.</returns>
    public string Format(string queueName);
}
