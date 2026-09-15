using System;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Creates Azure Service Bus queue or topic settings for send destinations.</summary>
public class ServiceBusSendTopology :
    SendTopology,
    IServiceBusSendTopologyConfigurator
{
    /// <summary>Gets or sets the callback applied to generated error-queue settings.</summary>
    public Action<IServiceBusEntityConfigurator>? ConfigureErrorSettings { get; set; }
    /// <summary>Gets or sets the callback applied to generated skipped-message queue settings.</summary>
    public Action<IServiceBusEntityConfigurator>? ConfigureDeadLetterSettings { get; set; }

    IServiceBusMessageSendTopology<T> IServiceBusSendTopology.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>() as IServiceBusMessageSendTopologyConfigurator<T>
            ?? throw new InvalidOperationException($"The send topology for {typeof(T).FullName} is not an Azure Service Bus topology.");
    }

    IServiceBusMessageSendTopologyConfigurator<T> IServiceBusSendTopologyConfigurator.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>() as IServiceBusMessageSendTopologyConfigurator<T>
            ?? throw new InvalidOperationException($"The send topology for {typeof(T).FullName} is not an Azure Service Bus topology.");
    }

    /// <summary>Builds queue or topic sender settings from an endpoint address.</summary>
    /// <param name="address">The parsed Azure Service Bus endpoint address.</param>
    /// <returns>The resolved entity and sender settings.</returns>
    public SendSettings GetSendSettings(ServiceBusEndpointAddress address)
    {
        if (address.Type == ServiceBusEndpointAddress.AddressType.Queue)
        {
            var createQueueOptions = GetCreateQueueOptions(address);

            return new QueueSendSettings(createQueueOptions);
        }

        var createTopicOptions = GetCreateTopicOptions(address);

        var builder = new BrokerTopologyBuilder();
        builder.CreateTopic(createTopicOptions);

        return new TopicSendSettings(createTopicOptions, builder.BuildBrokerTopology());
    }

    /// <summary>Builds error-queue settings from a source queue configuration.</summary>
    /// <param name="configurator">The source queue configuration.</param>
    /// <returns>The generated error-queue settings.</returns>
    public SendSettings GetErrorSettings(IServiceBusQueueConfigurator configurator)
    {
        var createQueueOptions = configurator.GetCreateQueueOptions();
        createQueueOptions.Name = ErrorQueueNameFormatter.FormatErrorQueueName(createQueueOptions.Name);

        var errorSettings = new QueueSendSettings(createQueueOptions);

        ConfigureErrorSettings?.Invoke(errorSettings);

        return errorSettings;
    }

    /// <summary>Builds skipped-message queue settings from a source queue configuration.</summary>
    /// <param name="configurator">The source queue configuration.</param>
    /// <returns>The generated skipped-message queue settings.</returns>
    public SendSettings GetDeadLetterSettings(IServiceBusQueueConfigurator configurator)
    {
        var createQueueOptions = configurator.GetCreateQueueOptions();
        createQueueOptions.Name = DeadLetterQueueNameFormatter.FormatDeadLetterQueueName(createQueueOptions.Name);

        var deadLetterSetting = new QueueSendSettings(createQueueOptions);

        ConfigureDeadLetterSettings?.Invoke(deadLetterSetting);

        return deadLetterSetting;
    }

    /// <summary>Creates send topology for a message contract.</summary>
    /// <typeparam name="T">The sent message contract.</typeparam>
    /// <returns>The message-specific Azure send topology.</returns>
    protected override IMessageSendTopologyConfigurator CreateMessageTopology<T>()
    {
        var messageTopology = new ServiceBusMessageSendTopology<T>();

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }

    static CreateQueueOptions GetCreateQueueOptions(ServiceBusEndpointAddress address)
    {
        var createQueueOptions = Defaults.GetCreateQueueOptions(address.Path);

        if (address.AutoDelete.HasValue)
            createQueueOptions.AutoDeleteOnIdle = address.AutoDelete.Value;

        return createQueueOptions;
    }

    static CreateTopicOptions GetCreateTopicOptions(ServiceBusEndpointAddress address)
    {
        var createTopicOptions = Defaults.GetCreateTopicOptions(address.Path);

        if (address.AutoDelete.HasValue)
            createTopicOptions.AutoDeleteOnIdle = address.AutoDelete.Value;

        return createTopicOptions;
    }
}
