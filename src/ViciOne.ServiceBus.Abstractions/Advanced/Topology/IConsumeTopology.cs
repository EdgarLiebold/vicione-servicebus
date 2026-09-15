using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Provides message-specific topology applied by a receive endpoint.</summary>
public interface IConsumeTopology :
    IConsumeTopologyConfigurationObserverConnector
{
    /// <summary>Gets the consume topology for a message contract.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <returns>The message-specific consume topology.</returns>
    IMessageConsumeTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Creates a temporary endpoint name containing the specified diagnostic tag.</summary>
    /// <param name="tag">The non-empty tag included in the name.</param>
    /// <returns>The generated temporary queue name.</returns>
    string CreateTemporaryQueueName(string tag);
}
