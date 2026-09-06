using ViciOne.ServiceBus.RabbitMq;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Creates RabbitMQ send, error, and dead-letter settings.</summary>
public interface IRabbitMqSendTopology :
    ISendTopology
{
    /// <summary>Gets the validator applied to RabbitMQ entity names.</summary>
    IEntityNameValidator EntityNameValidator { get; }

    /// <summary>Gets send topology for a message contract.</summary>
    /// <typeparam name="T">The sent message contract type.</typeparam>
    /// <returns>The RabbitMQ send-topology configurator for <typeparamref name="T"/>.</returns>
    new IRabbitMqMessageSendTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Creates send settings for the specified <paramref name="address"/>.</summary>
    /// <param name="address">The destination address and its encoded topology options.</param>
    /// <returns>The RabbitMQ send settings.</returns>
    SendSettings GetSendSettings(RabbitMqEndpointAddress address);

    /// <summary>Creates error exchange and queue settings for a receive endpoint.</summary>
    /// <param name="settings">The source receive endpoint settings.</param>
    /// <returns>The RabbitMQ error transport settings.</returns>
    ErrorSettings GetErrorSettings(ReceiveSettings settings);

    /// <summary>Creates dead-letter exchange and queue settings for a receive endpoint.</summary>
    /// <param name="settings">The source receive endpoint settings.</param>
    /// <returns>The RabbitMQ dead-letter transport settings.</returns>
    DeadLetterSettings GetDeadLetterSettings(ReceiveSettings settings);
}
