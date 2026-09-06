using System.Collections.Generic;
using System.Threading;
using Apache.NMS;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Stores framework and ActiveMQ-native settings for a message being sent.</summary>
/// <typeparam name="T">The message type.</typeparam>
public class TransportActiveMqSendContext<T> :
    MessageSendContext<T>,
    ActiveMqSendContext<T>
    where T : class
{
    /// <summary>Creates an ActiveMQ send context for a message.</summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">The token associated with the send operation.</param>
    public TransportActiveMqSendContext(T message, CancellationToken cancellationToken)
        : base(message, cancellationToken)
    {
    }

    /// <summary>Gets or sets the JMSX message-group identifier.</summary>
    public string? GroupId { get; set; }
    /// <summary>Gets or sets the JMSX message-group sequence number.</summary>
    public int? GroupSequence { get; set; }
    /// <summary>Gets or sets the Apache NMS message priority.</summary>
    public MsgPriority? Priority { get; set; }
    /// <summary>Gets or sets the native reply destination.</summary>
    public IDestination? ReplyDestination { get; set; }

    /// <summary>Restores ActiveMQ priority and message-group settings from transport properties.</summary>
    /// <param name="properties">The transport-property snapshot.</param>
    public override void ReadPropertiesFrom(IReadOnlyDictionary<string, object> properties)
    {
        base.ReadPropertiesFrom(properties);

        Priority = ReadEnum<MsgPriority>(properties, ActiveMqTransportPropertyNames.Priority);
        GroupId = ReadString(properties, ActiveMqTransportPropertyNames.GroupId);
        GroupSequence = ReadInt(properties, ActiveMqTransportPropertyNames.GroupSequence);
    }

    /// <summary>Writes non-null ActiveMQ priority and message-group settings to transport properties.</summary>
    /// <param name="properties">The transport-property destination.</param>
    public override void WritePropertiesTo(IDictionary<string, object> properties)
    {
        base.WritePropertiesTo(properties);

        if (Priority != null)
            properties[ActiveMqTransportPropertyNames.Priority] = Priority.Value.ToString();
        if (GroupId != null)
            properties[ActiveMqTransportPropertyNames.GroupId] = GroupId;
        if (GroupSequence != null)
            properties[ActiveMqTransportPropertyNames.GroupSequence] = GroupSequence.Value;
    }
}
