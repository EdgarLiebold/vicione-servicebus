using System;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>
/// Defines the contract for receive transport event.
/// </summary>
public interface ReceiveTransportEvent
{
    /// <summary>
    /// The input address of the receive endpoint
    /// </summary>
    Uri InputAddress { get; }
}
