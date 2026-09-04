using System;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>
/// Defines the contract for receive endpoint event.
/// </summary>
public interface ReceiveEndpointEvent
{
    /// <summary>
    /// The input address of the receive endpoint
    /// </summary>
    Uri InputAddress { get; }

    /// <summary>
    /// The receive endpoint upon which the event occurred
    /// </summary>
    IReceiveEndpoint ReceiveEndpoint { get; }
}
