using System;

namespace ViciOne.ServiceBus.Events;

/// <summary>
/// Provides a receive endpoint faulted event implementation.
/// </summary>
public class ReceiveEndpointFaultedEvent :
    ReceiveEndpointFaulted
{
    readonly ReceiveTransportFaulted _faulted;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="faulted">The faulted value.</param>
    /// <param name="receiveEndpoint">The receive endpoint value.</param>
    public ReceiveEndpointFaultedEvent(ReceiveTransportFaulted faulted, IReceiveEndpoint receiveEndpoint)
    {
        _faulted = faulted;
        ReceiveEndpoint = receiveEndpoint;
    }

    /// <summary>
    /// Gets the input address value.
    /// </summary>
    public Uri InputAddress => _faulted.InputAddress;
    /// <summary>
    /// Gets the exception value.
    /// </summary>
    public Exception Exception => _faulted.Exception;
    /// <summary>
    /// Gets the is terminal value.
    /// </summary>
    public bool IsTerminal => _faulted.IsTerminal;

    /// <summary>
    /// Gets the receive endpoint value.
    /// </summary>
    public IReceiveEndpoint ReceiveEndpoint { get; }
}
