namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for consumer fault context.
/// </summary>
public interface ConsumerFaultContext
{
    /// <summary>
    /// Gets the message type value.
    /// </summary>
    string MessageType { get; }
    /// <summary>
    /// Gets the consumer type value.
    /// </summary>
    string ConsumerType { get; }
}
