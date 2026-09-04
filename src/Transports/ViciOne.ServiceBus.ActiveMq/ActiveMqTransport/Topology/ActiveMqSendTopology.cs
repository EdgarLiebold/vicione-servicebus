using System;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>
/// Provides an active mq send topology implementation.
/// </summary>
public class ActiveMqSendTopology :
    SendTopology,
    IActiveMqSendTopologyConfigurator
{
    /// <summary>
    /// Gets or sets the configure error settings value.
    /// </summary>
    public Action<IActiveMqQueueConfigurator>? ConfigureErrorSettings { get; set; }
    /// <summary>
    /// Gets or sets the configure dead letter settings value.
    /// </summary>
    public Action<IActiveMqQueueConfigurator>? ConfigureDeadLetterSettings { get; set; }

    IActiveMqMessageSendTopologyConfigurator<T> IActiveMqSendTopology.GetMessageTopology<T>()
    {
        IMessageSendTopologyConfigurator<T> configurator = base.GetMessageTopology<T>();

        return configurator as IActiveMqMessageSendTopologyConfigurator<T>
            ?? throw new InvalidOperationException($"The send topology for {typeof(T).FullName} is not an ActiveMQ topology.");
    }

    /// <summary>
    /// Gets send settings.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    public SendSettings GetSendSettings(ActiveMqEndpointAddress address)
    {
        if (address.Type == ActiveMqEndpointAddress.AddressType.Queue)
            return new ActiveMqQueueSendSettings(address);

        return new ActiveMqTopicSendSettings(address);
    }

    /// <summary>
    /// Gets error settings.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <returns>The result of the operation.</returns>
    public ErrorSettings GetErrorSettings(EntitySettings settings)
    {
        var errorSettings = new ActiveMqErrorSettings(settings, ErrorQueueNameFormatter.FormatErrorQueueName(settings.EntityName));

        ConfigureErrorSettings?.Invoke(errorSettings);

        return errorSettings;
    }

    /// <summary>
    /// Gets dead letter settings.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <returns>The result of the operation.</returns>
    public DeadLetterSettings GetDeadLetterSettings(EntitySettings settings)
    {
        var deadLetterSetting = new ActiveMqDeadLetterSettings(settings, DeadLetterQueueNameFormatter.FormatDeadLetterQueueName(settings.EntityName));

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
        var messageTopology = new ActiveMqMessageSendTopology<T>();

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }
}
