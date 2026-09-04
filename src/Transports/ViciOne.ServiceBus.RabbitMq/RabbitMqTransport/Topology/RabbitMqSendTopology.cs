using System;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>
/// Provides a rabbit mq send topology implementation.
/// </summary>
public class RabbitMqSendTopology :
    SendTopology,
    IRabbitMqSendTopologyConfigurator
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="validator">The validator value.</param>
    public RabbitMqSendTopology(IEntityNameValidator validator)
    {
        ExchangeTypeSelector = new FanoutExchangeTypeSelector();
        EntityNameValidator = validator;
    }

    /// <summary>
    /// Gets the exchange type selector value.
    /// </summary>
    public IExchangeTypeSelector ExchangeTypeSelector { get; }
    /// <summary>
    /// Gets the entity name validator value.
    /// </summary>
    public IEntityNameValidator EntityNameValidator { get; }

    /// <summary>
    /// Gets or sets the configure error settings value.
    /// </summary>
    public Action<IRabbitMqQueueBindingConfigurator>? ConfigureErrorSettings { get; set; }
    /// <summary>
    /// Gets or sets the configure dead letter settings value.
    /// </summary>
    public Action<IRabbitMqQueueBindingConfigurator>? ConfigureDeadLetterSettings { get; set; }

    IRabbitMqMessageSendTopologyConfigurator<T> IRabbitMqSendTopology.GetMessageTopology<T>()
    {
        IMessageSendTopologyConfigurator<T> configurator = base.GetMessageTopology<T>();

        return configurator as IRabbitMqMessageSendTopologyConfigurator<T>
            ?? throw new InvalidOperationException($"The message topology for '{typeof(T)}' is not a RabbitMQ send topology.");
    }

    /// <summary>
    /// Gets send settings.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    public SendSettings GetSendSettings(RabbitMqEndpointAddress address)
    {
        return new RabbitMqSendSettings(address);
    }

    // TODO this is a smell, send for error/dead-letter settings?
    /// <summary>
    /// Gets error settings.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <returns>The result of the operation.</returns>
    public ErrorSettings GetErrorSettings(ReceiveSettings settings)
    {
        var errorSettings = new RabbitMqErrorSettings(settings, ErrorQueueNameFormatter.FormatErrorQueueName(settings.ExchangeName));

        ConfigureErrorSettings?.Invoke(errorSettings);

        return errorSettings;
    }

    /// <summary>
    /// Gets dead letter settings.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <returns>The result of the operation.</returns>
    public DeadLetterSettings GetDeadLetterSettings(ReceiveSettings settings)
    {
        var deadLetterSetting = new RabbitMqDeadLetterSettings(settings, DeadLetterQueueNameFormatter.FormatDeadLetterQueueName(settings.ExchangeName));

        ConfigureDeadLetterSettings?.Invoke(deadLetterSetting);

        return deadLetterSetting;
    }

    /// <summary>
    /// Creates message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the operation.</returns>
    protected override IMessageSendTopologyConfigurator CreateMessageTopology<T>(Type type)
    {
        var messageTopology = new RabbitMqMessageSendTopology<T>();

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }
}
