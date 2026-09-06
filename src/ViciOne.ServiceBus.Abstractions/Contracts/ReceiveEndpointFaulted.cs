using System;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Defines the operations required by receive endpoint faulted.</summary>
public interface ReceiveEndpointFaulted :
    ReceiveEndpointEvent
{
    /// <summary>Gets the exception.</summary>
    Exception Exception { get; }
    /// <summary>Gets a value indicating whether terminal.</summary>
    bool IsTerminal { get; }
}
