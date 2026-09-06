using System;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>
/// Provides a sql send topology implementation.
/// </summary>
public class SqlSendTopology :
    SendTopology,
    ISqlSendTopologyConfigurator
{
    /// <summary>
    /// Gets or sets the configure error settings value.
    /// </summary>
    public Action<ISqlQueueConfigurator>? ConfigureErrorSettings { get; set; }
    /// <summary>
    /// Gets or sets the configure dead letter settings value.
    /// </summary>
    public Action<ISqlQueueConfigurator>? ConfigureDeadLetterSettings { get; set; }

    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public new ISqlMessageSendTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class
    {
        IMessageSendTopologyConfigurator<T> configurator = base.GetMessageTopology<T>();

        return (configurator as ISqlMessageSendTopologyConfigurator<T>)!;
    }

    /// <summary>
    /// Gets send settings.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    public SendSettings GetSendSettings(SqlEndpointAddress address)
    {
        return address.Type == SqlEndpointAddress.AddressType.Queue
            ? new QueueSendSettings(address)
            : new TopicSendSettings(address);
    }

    /// <summary>
    /// Gets error settings.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <returns>The result of the operation.</returns>
    public SendSettings GetErrorSettings(ReceiveSettings settings)
    {
        var errorSettings = new QueueSendSettings(settings, ErrorQueueNameFormatter.FormatErrorQueueName(settings.QueueName));

        ConfigureErrorSettings?.Invoke(errorSettings);

        return errorSettings;
    }

    /// <summary>
    /// Gets dead letter settings.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <returns>The result of the operation.</returns>
    public SendSettings GetDeadLetterSettings(ReceiveSettings settings)
    {
        var deadLetterSetting = new QueueSendSettings(settings, DeadLetterQueueNameFormatter.FormatDeadLetterQueueName(settings.QueueName));

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
        var messageTopology = new SqlMessageSendTopology<T>();

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }
}
