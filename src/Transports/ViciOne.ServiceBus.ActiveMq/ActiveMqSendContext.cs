using Apache.NMS;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Exposes ActiveMQ-native send settings for a typed message.</summary>
/// <typeparam name="T">The message type.</typeparam>
public interface ActiveMqSendContext<out T> :
    ActiveMqSendContext,
    SendContext<T>
    where T : class
{
}


/// <summary>Exposes ActiveMQ-native send settings.</summary>
public interface ActiveMqSendContext :
    SendContext
{
    /// <summary>Sets the Apache NMS message priority.</summary>
    MsgPriority? Priority { set; }
    /// <summary>Sets the JMSX message-group identifier.</summary>
    string? GroupId { set; }
    /// <summary>Sets the JMSX message-group sequence number.</summary>
    int? GroupSequence { set; }

    /// <summary>Gets or sets the native reply destination.</summary>
    IDestination? ReplyDestination { get; set; }
}
