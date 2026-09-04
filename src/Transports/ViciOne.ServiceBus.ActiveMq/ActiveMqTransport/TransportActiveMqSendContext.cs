using System.Collections.Generic;
using System.Threading;
using Apache.NMS;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Provides a transport active mq send context implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class TransportActiveMqSendContext<T> :
    MessageSendContext<T>,
    ActiveMqSendContext<T>
    where T : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public TransportActiveMqSendContext(T message, CancellationToken cancellationToken)
        : base(message, cancellationToken)
    {
    }

    /// <summary>
    /// Gets or sets the group id value.
    /// </summary>
    public string? GroupId { get; set; }
    /// <summary>
    /// Gets or sets the group sequence value.
    /// </summary>
    public int? GroupSequence { get; set; }
    /// <summary>
    /// Gets or sets the priority value.
    /// </summary>
    public MsgPriority? Priority { get; set; }
    /// <summary>
    /// Gets or sets the reply destination value.
    /// </summary>
    public IDestination? ReplyDestination { get; set; }

    /// <summary>
    /// Performs the read properties from operation.
    /// </summary>
    /// <param name="properties">The properties value.</param>
    public override void ReadPropertiesFrom(IReadOnlyDictionary<string, object> properties)
    {
        base.ReadPropertiesFrom(properties);

        Priority = ReadEnum<MsgPriority>(properties, ActiveMqTransportPropertyNames.Priority);
        GroupId = ReadString(properties, ActiveMqTransportPropertyNames.GroupId);
        GroupSequence = ReadInt(properties, ActiveMqTransportPropertyNames.GroupSequence);
    }

    /// <summary>
    /// Performs the write properties to operation.
    /// </summary>
    /// <param name="properties">The properties value.</param>
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
