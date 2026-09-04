using System;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.ActiveMqTransport.Topology;

public class ActiveMqSendTopology :
    SendTopology,
    IActiveMqSendTopologyConfigurator
{
    public Action<IActiveMqQueueConfigurator> ConfigureErrorSettings { get; set; }
    public Action<IActiveMqQueueConfigurator> ConfigureDeadLetterSettings { get; set; }

    IActiveMqMessageSendTopologyConfigurator<T> IActiveMqSendTopology.GetMessageTopology<T>()
    {
        IMessageSendTopologyConfigurator<T> configurator = base.GetMessageTopology<T>();

        return configurator as IActiveMqMessageSendTopologyConfigurator<T>;
    }

    public SendSettings GetSendSettings(ActiveMqEndpointAddress address)
    {
        if (address.Type == ActiveMqEndpointAddress.AddressType.Queue)
            return new ActiveMqQueueSendSettings(address);

        return new ActiveMqTopicSendSettings(address);
    }

    public ErrorSettings GetErrorSettings(EntitySettings settings)
    {
        var errorSettings = new ActiveMqErrorSettings(settings, ErrorQueueNameFormatter.FormatErrorQueueName(settings.EntityName));

        ConfigureErrorSettings?.Invoke(errorSettings);

        return errorSettings;
    }

    public DeadLetterSettings GetDeadLetterSettings(EntitySettings settings)
    {
        var deadLetterSetting = new ActiveMqDeadLetterSettings(settings, DeadLetterQueueNameFormatter.FormatDeadLetterQueueName(settings.EntityName));

        ConfigureDeadLetterSettings?.Invoke(deadLetterSetting);

        return deadLetterSetting;
    }

    protected override IMessageSendTopologyConfigurator CreateMessageTopology<T>(Type type)
    {
        var messageTopology = new ActiveMqMessageSendTopology<T>();

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }
}
