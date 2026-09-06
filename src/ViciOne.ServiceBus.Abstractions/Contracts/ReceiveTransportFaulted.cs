using System;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Defines the operations required by receive transport faulted.</summary>
public interface ReceiveTransportFaulted :
    ReceiveTransportEvent
{
    /// <summary>Gets the exception.</summary>
    Exception Exception { get; }
    /// <summary>Gets a value indicating whether terminal.</summary>
    bool IsTerminal { get; }
}
