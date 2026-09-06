using System;
using System.Globalization;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Configures Amazon SQS queue topology for send, error, and skipped-message destinations.</summary>
public class AmazonSqsSendTopology :
    SendTopology,
    IAmazonSqsSendTopologyConfigurator
{
    /// <summary>Initializes Amazon SQS send topology.</summary>
    /// <param name="validator">The queue entity-name validator.</param>
    public AmazonSqsSendTopology(IEntityNameValidator validator)
    {
        EntityNameValidator = validator;
    }

    /// <summary>Gets the queue entity-name validator.</summary>
    public IEntityNameValidator EntityNameValidator { get; }

    /// <summary>Gets or sets the callback applied to generated error-queue settings.</summary>
    public Action<IAmazonSqsQueueConfigurator>? ConfigureErrorSettings { get; set; }
    /// <summary>Gets or sets the callback applied to generated skipped-message queue settings.</summary>
    public Action<IAmazonSqsQueueConfigurator>? ConfigureDeadLetterSettings { get; set; }

    IAmazonSqsMessageSendTopologyConfigurator<T> IAmazonSqsSendTopology.GetMessageTopology<T>()
    {
        IMessageSendTopologyConfigurator<T> configurator = base.GetMessageTopology<T>();

        return (configurator as IAmazonSqsMessageSendTopologyConfigurator<T>)!;
    }

    /// <summary>Creates queue send settings from an Amazon SQS endpoint address.</summary>
    /// <param name="address">The destination queue address.</param>
    /// <returns>The queue send settings.</returns>
    public SendSettings GetSendSettings(AmazonSqsEndpointAddress address)
    {
        return new QueueSendSettings(address);
    }

    /// <summary>Creates configurable error-queue settings from receive settings.</summary>
    /// <param name="settings">The source receive settings.</param>
    /// <returns>The generated error-queue settings.</returns>
    public ErrorSettings GetErrorSettings(ReceiveSettings settings)
    {
        var errorSettings = new QueueErrorSettings(settings, BuildEntityName(settings.EntityName, x => ErrorQueueNameFormatter.FormatErrorQueueName(x)));

        ConfigureErrorSettings?.Invoke(errorSettings);

        return errorSettings;
    }

    /// <summary>Creates configurable skipped-message queue settings from receive settings.</summary>
    /// <param name="settings">The source receive settings.</param>
    /// <returns>The generated skipped-message queue settings.</returns>
    public DeadLetterSettings GetDeadLetterSettings(ReceiveSettings settings)
    {
        var deadLetterSetting = new QueueDeadLetterSettings(settings,
            BuildEntityName(settings.EntityName, x => DeadLetterQueueNameFormatter.FormatDeadLetterQueueName(x)));

        ConfigureDeadLetterSettings?.Invoke(deadLetterSetting);

        return deadLetterSetting;
    }

    /// <summary>Creates and announces Amazon SQS send topology for a message type.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="type">The runtime message type represented by the topology.</param>
    /// <returns>The message send-topology configurator.</returns>
    protected override IMessageSendTopologyConfigurator CreateMessageTopology<T>(Type type)
    {
        var messageTopology = new AmazonSqsMessageSendTopology<T>();

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }

    static string BuildEntityName(string entityName, Func<string, string> formatQueueName)
    {
        const string fifoSuffix = ".fifo";

        if (!entityName.EndsWith(fifoSuffix, true, CultureInfo.InvariantCulture))
            return formatQueueName(entityName);

        return formatQueueName(entityName.Substring(0, entityName.Length - fifoSuffix.Length)) + fifoSuffix;
    }
}
