using System;

namespace ViciOne.ServiceBus.Events;

/// <summary>
/// Provides a receive endpoint stopping event implementation.
/// </summary>
public class ReceiveEndpointStoppingEvent :
    ReceiveEndpointStopping
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="inputAddress">The input address value.</param>
    /// <param name="receiveEndpoint">The receive endpoint value.</param>
    /// <param name="removed">The removed value.</param>
    public ReceiveEndpointStoppingEvent(Uri inputAddress, IReceiveEndpoint receiveEndpoint, bool removed)
    {
        InputAddress = inputAddress;
        ReceiveEndpoint = receiveEndpoint;
        Removed = removed;
    }

    /// <summary>
    /// Gets the removed value.
    /// </summary>
    public bool Removed { get; }

    /// <summary>
    /// Gets the input address value.
    /// </summary>
    public Uri InputAddress { get; }

    /// <summary>
    /// Gets the receive endpoint value.
    /// </summary>
    public IReceiveEndpoint ReceiveEndpoint { get; }
}
