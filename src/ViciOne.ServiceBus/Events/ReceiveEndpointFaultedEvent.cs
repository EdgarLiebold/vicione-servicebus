using System;

namespace ViciOne.ServiceBus.Events;

/// <summary>Carries the receive endpoint faulted event data.</summary>
public class ReceiveEndpointFaultedEvent :
    ReceiveEndpointFaulted
{
    readonly ReceiveTransportFaulted _faulted;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="faulted">The faulted.</param>
    /// <param name="receiveEndpoint">The receive endpoint.</param>
    public ReceiveEndpointFaultedEvent(ReceiveTransportFaulted faulted, IReceiveEndpoint receiveEndpoint)
    {
        _faulted = faulted;
        ReceiveEndpoint = receiveEndpoint;
    }

    /// <summary>Gets the input address.</summary>
    public Uri InputAddress => _faulted.InputAddress;
    /// <summary>Gets the exception.</summary>
    public Exception Exception => _faulted.Exception;
    /// <summary>Gets a value indicating whether terminal.</summary>
    public bool IsTerminal => _faulted.IsTerminal;

    /// <summary>Gets the receive endpoint.</summary>
    public IReceiveEndpoint ReceiveEndpoint { get; }
}
