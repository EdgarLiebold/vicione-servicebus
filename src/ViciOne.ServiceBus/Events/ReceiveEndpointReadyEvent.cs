// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Events
{
    using System;


    public class ReceiveEndpointReadyEvent :
        ReceiveEndpointReady
    {
        public ReceiveEndpointReadyEvent(Uri inputAddress, IReceiveEndpoint receiveEndpoint, bool isStarted)
        {
            InputAddress = inputAddress;
            ReceiveEndpoint = receiveEndpoint;
            IsStarted = isStarted;
        }

        public Uri InputAddress { get; }

        public IReceiveEndpoint ReceiveEndpoint { get; }

        public bool IsStarted { get; }
    }
}
