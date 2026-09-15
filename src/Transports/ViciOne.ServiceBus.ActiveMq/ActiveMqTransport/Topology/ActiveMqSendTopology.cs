using System;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Creates ActiveMQ destination settings and per-message send topology.</summary>
public class ActiveMqSendTopology :
    SendTopology,
    IActiveMqSendTopologyConfigurator
{
    /// <summary>Gets or sets the callback applied to generated error-queue settings.</summary>
    public Action<IActiveMqQueueConfigurator>? ConfigureErrorSettings { get; set; }
    /// <summary>Gets or sets the callback applied to generated dead-letter queue settings.</summary>
    public Action<IActiveMqQueueConfigurator>? ConfigureDeadLetterSettings { get; set; }

    IActiveMqMessageSendTopologyConfigurator<T> IActiveMqSendTopology.GetMessageTopology<T>()
    {
        IMessageSendTopologyConfigurator<T> configurator = base.GetMessageTopology<T>();

        return configurator as IActiveMqMessageSendTopologyConfigurator<T>
            ?? throw new InvalidOperationException($"The send topology for {typeof(T).FullName} is not an ActiveMQ topology.");
    }

    /// <summary>Creates queue or topic send settings for an endpoint address.</summary>
    /// <param name="address">The parsed ActiveMQ destination address.</param>
    /// <returns>The destination-specific send settings.</returns>
    public SendSettings GetSendSettings(ActiveMqEndpointAddress address)
    {
        if (address.Type == ActiveMqEndpointAddress.AddressType.Queue)
            return new ActiveMqQueueSendSettings(address);

        return new ActiveMqTopicSendSettings(address);
    }

    /// <summary>Creates error-queue settings derived from a source entity.</summary>
    /// <param name="settings">The source entity settings.</param>
    /// <returns>The configured error-destination settings.</returns>
    public ErrorSettings GetErrorSettings(EntitySettings settings)
    {
        var errorSettings = new ActiveMqErrorSettings(settings, ErrorQueueNameFormatter.FormatErrorQueueName(settings.EntityName));

        ConfigureErrorSettings?.Invoke(errorSettings);

        return errorSettings;
    }

    /// <summary>Creates dead-letter queue settings derived from a source entity.</summary>
    /// <param name="settings">The source entity settings.</param>
    /// <returns>The configured dead-letter destination settings.</returns>
    public DeadLetterSettings GetDeadLetterSettings(EntitySettings settings)
    {
        var deadLetterSetting = new ActiveMqDeadLetterSettings(settings, DeadLetterQueueNameFormatter.FormatDeadLetterQueueName(settings.EntityName));

        ConfigureDeadLetterSettings?.Invoke(deadLetterSetting);

        return deadLetterSetting;
    }

    /// <summary>Creates ActiveMQ send topology for a message type.</summary>
    /// <typeparam name="T">The sent message type.</typeparam>
    /// <returns>The new message send topology.</returns>
    protected override IMessageSendTopologyConfigurator CreateMessageTopology<T>()
    {
        var messageTopology = new ActiveMqMessageSendTopology<T>();

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }
}
