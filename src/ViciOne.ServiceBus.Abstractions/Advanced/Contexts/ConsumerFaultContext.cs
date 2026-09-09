namespace ViciOne.ServiceBus.Advanced;

/// <summary>Identifies the message and consumer contracts associated with a consumer fault.</summary>
public interface ConsumerFaultContext
{
    /// <summary>Gets the serialized message-contract identifier.</summary>
    string MessageType { get; }
    /// <summary>Gets the serialized consumer-type identifier.</summary>
    string ConsumerType { get; }
}
