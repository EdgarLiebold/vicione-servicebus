using Apache.NMS;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for active mq message context.
/// </summary>
public interface ActiveMqMessageContext
{
    /// <summary>
    /// Gets the transport message value.
    /// </summary>
    IMessage TransportMessage { get; }

    /// <summary>
    /// Gets the properties value.
    /// </summary>
    IPrimitiveMap Properties { get; }

    /// <summary>
    /// Gets the group id value.
    /// </summary>
    string? GroupId { get; }
    /// <summary>
    /// Gets the group sequence value.
    /// </summary>
    int GroupSequence { get; }
}
