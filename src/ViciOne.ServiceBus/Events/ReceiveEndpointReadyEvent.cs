using System;

namespace ViciOne.ServiceBus.Events;

/// <summary>
/// Provides a receive endpoint ready event implementation.
/// </summary>
public class ReceiveEndpointReadyEvent :
    ReceiveEndpointReady
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="inputAddress">The input address value.</param>
    /// <param name="receiveEndpoint">The receive endpoint value.</param>
    /// <param name="isStarted">The is started value.</param>
    public ReceiveEndpointReadyEvent(Uri inputAddress, IReceiveEndpoint receiveEndpoint, bool isStarted)
    {
        InputAddress = inputAddress;
        ReceiveEndpoint = receiveEndpoint;
        IsStarted = isStarted;
    }

    /// <summary>
    /// Gets the input address value.
    /// </summary>
    public Uri InputAddress { get; }

    /// <summary>
    /// Gets the receive endpoint value.
    /// </summary>
    public IReceiveEndpoint ReceiveEndpoint { get; }

    /// <summary>
    /// Gets the is started value.
    /// </summary>
    public bool IsStarted { get; }
}
