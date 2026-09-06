using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Defines the operations required by send topology.</summary>
public interface ISendTopology :
    ISendTopologyConfigurationObserverConnector
{
    /// <summary>Gets the dead letter queue name formatter.</summary>
    IDeadLetterQueueNameFormatter DeadLetterQueueNameFormatter { get; }

    /// <summary>Gets the error queue name formatter.</summary>
    IErrorQueueNameFormatter ErrorQueueNameFormatter { get; }

    /// <summary>Returns the specification for the message type.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <returns>The message topology.</returns>
    IMessageSendTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;
}
