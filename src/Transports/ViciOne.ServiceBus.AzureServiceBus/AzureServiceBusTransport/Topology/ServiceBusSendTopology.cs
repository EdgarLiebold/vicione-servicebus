using System;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// Provides a service bus send topology implementation.
/// </summary>
public class ServiceBusSendTopology :
    SendTopology,
    IServiceBusSendTopologyConfigurator
{
    /// <summary>
    /// Gets or sets the configure error settings value.
    /// </summary>
    public Action<IServiceBusEntityConfigurator>? ConfigureErrorSettings { get; set; }
    /// <summary>
    /// Gets or sets the configure dead letter settings value.
    /// </summary>
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

    /// <summary>
    /// Gets send settings.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Gets error settings.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <returns>The result of the operation.</returns>
    public SendSettings GetErrorSettings(IServiceBusQueueConfigurator configurator)
    {
        var createQueueOptions = configurator.GetCreateQueueOptions();
        createQueueOptions.Name = ErrorQueueNameFormatter.FormatErrorQueueName(createQueueOptions.Name);

        var errorSettings = new QueueSendSettings(createQueueOptions);

        ConfigureErrorSettings?.Invoke(errorSettings);

        return errorSettings;
    }

    /// <summary>
    /// Gets dead letter settings.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <returns>The result of the operation.</returns>
    public SendSettings GetDeadLetterSettings(IServiceBusQueueConfigurator configurator)
    {
        var createQueueOptions = configurator.GetCreateQueueOptions();
        createQueueOptions.Name = DeadLetterQueueNameFormatter.FormatDeadLetterQueueName(createQueueOptions.Name);

        var deadLetterSetting = new QueueSendSettings(createQueueOptions);

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
