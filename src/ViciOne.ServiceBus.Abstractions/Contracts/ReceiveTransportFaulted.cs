using System;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Describes a recoverable or terminal receive-transport failure.</summary>
public interface ReceiveTransportFaulted :
    ReceiveTransportEvent
{
    /// <summary>Gets the transport failure.</summary>
    Exception Exception { get; }
    /// <summary>Gets whether the transport exhausted recovery and will not retry.</summary>
    bool IsTerminal { get; }
}
