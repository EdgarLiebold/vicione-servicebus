// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus.Events
{
    using System;


    public class ReceiveTransportFaultedEvent :
        ReceiveTransportFaulted
    {
        public ReceiveTransportFaultedEvent(Uri inputAddress, Exception exception)
        {
            InputAddress = inputAddress;
            Exception = exception;
        }

        public Uri InputAddress { get; }

        public Exception? Exception { get; }
    }
}
