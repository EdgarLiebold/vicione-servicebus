using ViciOne.ServiceBus.ActiveMq;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Exposes ActiveMQ destination, error, dead-letter, and per-message send topology.</summary>
public interface IActiveMqSendTopology :
    ISendTopology
{
    /// <summary>Gets configurable send topology for a message type.</summary>
    /// <typeparam name="T">The sent message type.</typeparam>
    /// <returns>The ActiveMQ message send-topology configurator.</returns>
    new IActiveMqMessageSendTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Creates queue or topic send settings for an endpoint address.</summary>
    /// <param name="address">The parsed ActiveMQ destination address.</param>
    /// <returns>The destination-specific send settings.</returns>
    SendSettings GetSendSettings(ActiveMqEndpointAddress address);

    /// <summary>Creates error-queue settings derived from a source entity.</summary>
    /// <param name="settings">The source entity settings.</param>
    /// <returns>The error-destination settings.</returns>
    ErrorSettings GetErrorSettings(EntitySettings settings);

    /// <summary>Creates dead-letter queue settings derived from a source entity.</summary>
    /// <param name="settings">The source entity settings.</param>
    /// <returns>The dead-letter destination settings.</returns>
    DeadLetterSettings GetDeadLetterSettings(EntitySettings settings);
}
