namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>
/// Defines the contract for receive endpoint stopping.
/// </summary>
public interface ReceiveEndpointStopping :
    ReceiveEndpointEvent
{
    /// <summary>
    /// Gets the removed value.
    /// </summary>
    bool Removed { get; }
}
