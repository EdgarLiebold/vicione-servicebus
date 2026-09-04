using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>
/// Defines the contract for send topology.
/// </summary>
public interface ISendTopology :
    ISendTopologyConfigurationObserverConnector
{
    /// <summary>
    /// Gets the dead letter queue name formatter value.
    /// </summary>
    IDeadLetterQueueNameFormatter DeadLetterQueueNameFormatter { get; }

    /// <summary>
    /// Gets the error queue name formatter value.
    /// </summary>
    IErrorQueueNameFormatter ErrorQueueNameFormatter { get; }

    /// <summary>
    /// Returns the specification for the message type
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <returns></returns>
    IMessageSendTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;
}
