// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus.Events
{
    using System;


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
        public Exception? Exception => _faulted.Exception;

        public IReceiveEndpoint ReceiveEndpoint { get; }
    }
}
