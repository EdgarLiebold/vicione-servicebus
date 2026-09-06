namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Defines the operations required by receive endpoint stopping.</summary>
public interface ReceiveEndpointStopping :
    ReceiveEndpointEvent
{
    /// <summary>Gets the removed.</summary>
    bool Removed { get; }
}
