namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Defines the operations required by receive transport ready.</summary>
public interface ReceiveTransportReady :
    ReceiveTransportEvent
{
    /// <summary>If true, the receive transport is actually ready, versus "fake-ready" for endpoints which do not auto-start.</summary>
    bool IsStarted { get; }
}
