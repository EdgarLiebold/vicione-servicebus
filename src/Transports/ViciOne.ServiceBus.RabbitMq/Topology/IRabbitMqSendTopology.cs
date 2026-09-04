using ViciOne.ServiceBus.RabbitMq;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Defines the contract for rabbit mq send topology.
/// </summary>
public interface IRabbitMqSendTopology :
    ISendTopology
{
    /// <summary>
    /// Gets the entity name validator value.
    /// </summary>
    IEntityNameValidator EntityNameValidator { get; }

    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IRabbitMqMessageSendTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Return the send settings for the specified <paramref name="address" />
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    SendSettings GetSendSettings(RabbitMqEndpointAddress address);

    /// <summary>
    /// Return the error settings for the queue
    /// </summary>
    /// <param name="settings"></param>
    /// <returns></returns>
    ErrorSettings GetErrorSettings(ReceiveSettings settings);

    /// <summary>
    /// Return the dead letter settings for the queue
    /// </summary>
    /// <param name="settings"></param>
    /// <returns></returns>
    DeadLetterSettings GetDeadLetterSettings(ReceiveSettings settings);
}
