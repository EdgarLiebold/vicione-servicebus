namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Describes a receive endpoint that is stopping or being removed.</summary>
public interface ReceiveEndpointStopping :
    ReceiveEndpointEvent
{
    /// <summary>Gets whether the endpoint is being removed from its host.</summary>
    bool Removed { get; }
}
