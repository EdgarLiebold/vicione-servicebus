using System;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>
/// Defines the contract for receive endpoint faulted.
/// </summary>
public interface ReceiveEndpointFaulted :
    ReceiveEndpointEvent
{
    /// <summary>
    /// Gets the exception value.
    /// </summary>
    Exception Exception { get; }
    /// <summary>
    /// Gets the is terminal value.
    /// </summary>
    bool IsTerminal { get; }
}
