using System;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Defines the topology for sql send.</summary>
public class SqlSendTopology :
    SendTopology,
    ISqlSendTopologyConfigurator
{
    /// <summary>Gets or sets the configure error settings.</summary>
    public Action<ISqlQueueConfigurator>? ConfigureErrorSettings { get; set; }
    /// <summary>Gets or sets the configure dead letter settings.</summary>
    public Action<ISqlQueueConfigurator>? ConfigureDeadLetterSettings { get; set; }

    /// <summary>Gets message topology.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message topology.</returns>
    public new ISqlMessageSendTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class
    {
        IMessageSendTopologyConfigurator<T> configurator = base.GetMessageTopology<T>();

        return (configurator as ISqlMessageSendTopologyConfigurator<T>)!;
    }

    /// <summary>Gets send settings.</summary>
    /// <param name="address">The address.</param>
    /// <returns>The send settings.</returns>
    public SendSettings GetSendSettings(SqlEndpointAddress address)
    {
        return address.Kind == SqlEndpointKind.Queue
            ? new QueueSendSettings(address)
            : new TopicSendSettings(address);
    }

    /// <summary>Gets error settings.</summary>
    /// <param name="settings">The settings that control the operation.</param>
    /// <returns>The error settings.</returns>
    public SendSettings GetErrorSettings(ReceiveSettings settings)
    {
        var errorSettings = new QueueSendSettings(settings, ErrorQueueNameFormatter.FormatErrorQueueName(settings.QueueName));

        ConfigureErrorSettings?.Invoke(errorSettings);

        return errorSettings;
    }

    /// <summary>Gets dead letter settings.</summary>
    /// <param name="settings">The settings that control the operation.</param>
    /// <returns>The dead letter settings.</returns>
    public SendSettings GetDeadLetterSettings(ReceiveSettings settings)
    {
        var deadLetterSetting = new QueueSendSettings(settings, DeadLetterQueueNameFormatter.FormatDeadLetterQueueName(settings.QueueName));

        ConfigureDeadLetterSettings?.Invoke(deadLetterSetting);

        return deadLetterSetting;
    }

    /// <summary>Creates SQL send topology for a message contract.</summary>
    /// <typeparam name="T">The sent message contract type.</typeparam>
    /// <returns>The SQL message send topology.</returns>
    protected override IMessageSendTopologyConfigurator CreateMessageTopology<T>()
    {
        var messageTopology = new SqlMessageSendTopology<T>();

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }
}
