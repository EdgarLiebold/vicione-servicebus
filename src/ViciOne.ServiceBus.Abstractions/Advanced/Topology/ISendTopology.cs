using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Provides message-specific topology and failure-queue naming for sends.</summary>
public interface ISendTopology :
    ISendTopologyConfigurationObserverConnector
{
    /// <summary>Gets the dead letter queue name formatter.</summary>
    IDeadLetterQueueNameFormatter DeadLetterQueueNameFormatter { get; }

    /// <summary>Gets the error queue name formatter.</summary>
    IErrorQueueNameFormatter ErrorQueueNameFormatter { get; }

    /// <summary>Gets the send topology for a message contract.</summary>
    /// <typeparam name="T">The sent message contract.</typeparam>
    /// <returns>The message-specific send topology.</returns>
    IMessageSendTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;
}
