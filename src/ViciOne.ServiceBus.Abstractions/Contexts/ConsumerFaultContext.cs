namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes state for consumer fault operations.</summary>
public interface ConsumerFaultContext
{
    /// <summary>Gets the message type.</summary>
    string MessageType { get; }
    /// <summary>Gets the consumer type.</summary>
    string ConsumerType { get; }
}
