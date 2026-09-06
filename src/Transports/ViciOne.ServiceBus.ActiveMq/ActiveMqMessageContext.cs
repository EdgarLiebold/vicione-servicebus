using Apache.NMS;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Exposes ActiveMQ-native state for a received message.</summary>
public interface ActiveMqMessageContext
{
    /// <summary>Gets the underlying Apache NMS message.</summary>
    IMessage TransportMessage { get; }

    /// <summary>Gets the message's native primitive-property map.</summary>
    IPrimitiveMap Properties { get; }

    /// <summary>Gets the JMSX message-group identifier.</summary>
    string? GroupId { get; }
    /// <summary>Gets the JMSX message-group sequence number.</summary>
    int GroupSequence { get; }
}
