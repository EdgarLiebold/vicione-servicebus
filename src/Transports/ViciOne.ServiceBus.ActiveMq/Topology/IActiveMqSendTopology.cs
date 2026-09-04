using ViciOne.ServiceBus.ActiveMq;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for active mq send topology.
/// </summary>
public interface IActiveMqSendTopology :
    ISendTopology
{
    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IActiveMqMessageSendTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Gets send settings.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    SendSettings GetSendSettings(ActiveMqEndpointAddress address);

    /// <summary>
    /// Return the error settings for the queue
    /// </summary>
    /// <param name="settings"></param>
    /// <returns></returns>
    ErrorSettings GetErrorSettings(EntitySettings settings);

    /// <summary>
    /// Return the dead letter settings for the queue
    /// </summary>
    /// <param name="settings"></param>
    /// <returns></returns>
    DeadLetterSettings GetDeadLetterSettings(EntitySettings settings);
}
