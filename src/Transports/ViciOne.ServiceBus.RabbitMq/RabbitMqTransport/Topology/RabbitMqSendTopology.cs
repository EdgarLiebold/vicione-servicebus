using System;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Creates RabbitMQ send, error, and dead-letter settings.</summary>
public class RabbitMqSendTopology :
    SendTopology,
    IRabbitMqSendTopologyConfigurator
{
    /// <summary>Creates send topology that uses fanout exchanges by default.</summary>
    /// <param name="validator">The validator applied to RabbitMQ entity names.</param>
    public RabbitMqSendTopology(IEntityNameValidator validator)
    {
        ExchangeTypeSelector = new FanoutExchangeTypeSelector();
        EntityNameValidator = validator;
    }

    /// <summary>Gets the selector used to determine exchange types for sent message contracts.</summary>
    public IExchangeTypeSelector ExchangeTypeSelector { get; }
    /// <summary>Gets the validator applied to RabbitMQ entity names.</summary>
    public IEntityNameValidator EntityNameValidator { get; }

    /// <summary>Gets or sets the callback that customizes the generated error exchange and queue.</summary>
    public Action<IRabbitMqQueueBindingConfigurator>? ConfigureErrorSettings { get; set; }
    /// <summary>Gets or sets the callback that customizes the generated dead-letter exchange and queue.</summary>
    public Action<IRabbitMqQueueBindingConfigurator>? ConfigureDeadLetterSettings { get; set; }

    IRabbitMqMessageSendTopologyConfigurator<T> IRabbitMqSendTopology.GetMessageTopology<T>()
    {
        IMessageSendTopologyConfigurator<T> configurator = base.GetMessageTopology<T>();

        return configurator as IRabbitMqMessageSendTopologyConfigurator<T>
            ?? throw new InvalidOperationException($"The message topology for '{typeof(T)}' is not a RabbitMQ send topology.");
    }

    /// <summary>Creates send settings from a RabbitMQ endpoint address.</summary>
    /// <param name="address">The destination address and its encoded topology options.</param>
    /// <returns>The RabbitMQ send settings.</returns>
    public SendSettings GetSendSettings(RabbitMqEndpointAddress address)
    {
        return new RabbitMqSendSettings(address);
    }

    /// <summary>Creates and customizes error-queue settings for a receive endpoint.</summary>
    /// <param name="settings">The source receive endpoint settings.</param>
    /// <returns>The RabbitMQ error transport settings.</returns>
    public ErrorSettings GetErrorSettings(ReceiveSettings settings)
    {
        var errorSettings = new RabbitMqErrorSettings(settings, ErrorQueueNameFormatter.FormatErrorQueueName(settings.ExchangeName));

        ConfigureErrorSettings?.Invoke(errorSettings);

        return errorSettings;
    }

    /// <summary>Creates and customizes dead-letter-queue settings for a receive endpoint.</summary>
    /// <param name="settings">The source receive endpoint settings.</param>
    /// <returns>The RabbitMQ dead-letter transport settings.</returns>
    public DeadLetterSettings GetDeadLetterSettings(ReceiveSettings settings)
    {
        var deadLetterSetting = new RabbitMqDeadLetterSettings(settings, DeadLetterQueueNameFormatter.FormatDeadLetterQueueName(settings.ExchangeName));

        ConfigureDeadLetterSettings?.Invoke(deadLetterSetting);

        return deadLetterSetting;
    }

    /// <summary>Creates provider-specific send topology for a message contract.</summary>
    /// <typeparam name="T">The sent message contract type.</typeparam>
    /// <param name="type">The runtime message contract type supplied by the base topology.</param>
    /// <returns>The RabbitMQ message send topology.</returns>
    protected override IMessageSendTopologyConfigurator CreateMessageTopology<T>(Type type)
    {
        var messageTopology = new RabbitMqMessageSendTopology<T>();

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }
}
