using System;

#nullable enable
namespace ViciOne.ServiceBus.Events;

public class ReceiveEndpointFaultedEvent :
    ReceiveEndpointFaulted
{
    readonly ReceiveTransportFaulted _faulted;

    public ReceiveEndpointFaultedEvent(ReceiveTransportFaulted faulted, IReceiveEndpoint receiveEndpoint)
    {
        _faulted = faulted;
        ReceiveEndpoint = receiveEndpoint;
    }

    public Uri InputAddress => _faulted.InputAddress;
    public Exception Exception => _faulted.Exception;
    public bool IsTerminal => _faulted.IsTerminal;

    public IReceiveEndpoint ReceiveEndpoint { get; }
}
