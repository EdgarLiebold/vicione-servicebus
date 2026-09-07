namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Describes a receive endpoint that is available to observers.</summary>
public interface ReceiveEndpointReady :
    ReceiveEndpointEvent
{
    /// <summary>Gets whether the readiness notification follows endpoint startup.</summary>
    bool IsStarted { get; }
}
