using System;

namespace ViciOne.ServiceBus.Events;

/// <summary>Projects a transport failure through its owning receive endpoint.</summary>
internal sealed class ReceiveEndpointFaultedEvent :
    ReceiveEndpointFaulted
{
    readonly ReceiveTransportFaulted _faulted;

    /// <summary>Creates an endpoint fault notification.</summary>
    /// <param name="faulted">The underlying transport failure.</param>
    /// <param name="receiveEndpoint">The endpoint that owns the failed transport.</param>
    public ReceiveEndpointFaultedEvent(ReceiveTransportFaulted faulted, IReceiveEndpoint receiveEndpoint)
    {
        _faulted = faulted ?? throw new ArgumentNullException(nameof(faulted));
        ReceiveEndpoint = receiveEndpoint ?? throw new ArgumentNullException(nameof(receiveEndpoint));
    }

    /// <summary>Gets the failed transport's input address.</summary>
    public Uri InputAddress => _faulted.InputAddress;
    /// <summary>Gets the transport failure.</summary>
    public Exception Exception => _faulted.Exception;
    /// <summary>Gets a value indicating whether the transport has exhausted recovery and will not retry.</summary>
    public bool IsTerminal => _faulted.IsTerminal;

    /// <summary>Gets the endpoint that owns the failed transport.</summary>
    public IReceiveEndpoint ReceiveEndpoint { get; }
}
