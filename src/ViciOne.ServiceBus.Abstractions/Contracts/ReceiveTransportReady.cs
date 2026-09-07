namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Describes a receive transport that is available to accept deliveries.</summary>
public interface ReceiveTransportReady :
    ReceiveTransportEvent
{
    /// <summary>Gets whether the readiness notification follows transport startup.</summary>
    bool IsStarted { get; }
}
