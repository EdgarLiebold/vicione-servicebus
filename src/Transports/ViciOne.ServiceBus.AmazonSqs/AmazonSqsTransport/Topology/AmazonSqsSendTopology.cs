using System;
using System.Globalization;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Provides an amazon sqs send topology implementation.
/// </summary>
public class AmazonSqsSendTopology :
    SendTopology,
    IAmazonSqsSendTopologyConfigurator
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="validator">The validator value.</param>
    public AmazonSqsSendTopology(IEntityNameValidator validator)
    {
        EntityNameValidator = validator;
    }

    /// <summary>
    /// Gets the entity name validator value.
    /// </summary>
    public IEntityNameValidator EntityNameValidator { get; }

    /// <summary>
    /// Gets or sets the configure error settings value.
    /// </summary>
    public Action<IAmazonSqsQueueConfigurator>? ConfigureErrorSettings { get; set; }
    /// <summary>
    /// Gets or sets the configure dead letter settings value.
    /// </summary>
    public Action<IAmazonSqsQueueConfigurator>? ConfigureDeadLetterSettings { get; set; }

    IAmazonSqsMessageSendTopologyConfigurator<T> IAmazonSqsSendTopology.GetMessageTopology<T>()
    {
        IMessageSendTopologyConfigurator<T> configurator = base.GetMessageTopology<T>();

        return (configurator as IAmazonSqsMessageSendTopologyConfigurator<T>)!;
    }

    /// <summary>
    /// Gets send settings.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    public SendSettings GetSendSettings(AmazonSqsEndpointAddress address)
    {
        return new QueueSendSettings(address);
    }

    /// <summary>
    /// Gets error settings.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <returns>The result of the operation.</returns>
    public ErrorSettings GetErrorSettings(ReceiveSettings settings)
    {
        var errorSettings = new QueueErrorSettings(settings, BuildEntityName(settings.EntityName, x => ErrorQueueNameFormatter.FormatErrorQueueName(x)));

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
        var deadLetterSetting = new QueueDeadLetterSettings(settings,
            BuildEntityName(settings.EntityName, x => DeadLetterQueueNameFormatter.FormatDeadLetterQueueName(x)));

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
