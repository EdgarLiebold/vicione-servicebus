using Apache.NMS;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for active mq send context.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface ActiveMqSendContext<out T> :
    ActiveMqSendContext,
    SendContext<T>
    where T : class
{
}


/// <summary>
/// Defines the contract for active mq send context.
/// </summary>
public interface ActiveMqSendContext :
    SendContext
{
    /// <summary>
    /// Gets or sets the priority value.
    /// </summary>
    MsgPriority? Priority { set; }
    /// <summary>
    /// Gets or sets the group id value.
    /// </summary>
    string? GroupId { set; }
    /// <summary>
    /// Gets or sets the group sequence value.
    /// </summary>
    int? GroupSequence { set; }

    /// <summary>
    /// Gets or sets the reply destination value.
    /// </summary>
    IDestination? ReplyDestination { get; set; }
}
