using System;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Describes a recoverable or terminal failure of a receive endpoint's transport.</summary>
public interface ReceiveEndpointFaulted :
    ReceiveEndpointEvent
{
    /// <summary>Gets the transport failure.</summary>
    Exception Exception { get; }
    /// <summary>Gets whether the transport exhausted recovery and will not retry.</summary>
    bool IsTerminal { get; }
}
