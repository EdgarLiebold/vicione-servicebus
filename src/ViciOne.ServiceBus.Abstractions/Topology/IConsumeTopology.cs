using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Defines the operations required by consume topology.</summary>
public interface IConsumeTopology :
    IConsumeTopologyConfigurationObserverConnector
{
    /// <summary>Returns the specification for the message type.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <returns>The message topology.</returns>
    IMessageConsumeTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Create a temporary endpoint name, using the specified tag.</summary>
    /// <param name="tag">The tag.</param>
    /// <returns>The created temporary queue name.</returns>
    string CreateTemporaryQueueName(string tag);
}
