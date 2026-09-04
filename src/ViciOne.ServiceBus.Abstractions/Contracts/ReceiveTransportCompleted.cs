namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>
/// Defines the contract for receive transport completed.
/// </summary>
public interface ReceiveTransportCompleted :
    ReceiveTransportEvent
{
    /// <summary>
    /// The number of messages delivered to the receive endpoint
    /// </summary>
    long DeliveryCount { get; }

    /// <summary>
    /// The maximum concurrent messages delivery to the receive endpoint
    /// </summary>
    long ConcurrentDeliveryCount { get; }
}
