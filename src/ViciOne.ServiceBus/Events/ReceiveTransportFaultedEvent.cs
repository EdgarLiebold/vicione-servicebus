#nullable enable
namespace ViciOne.ServiceBus.Events
{
    using System;


    public class ReceiveTransportFaultedEvent :
        ReceiveTransportFaulted
    {
        public ReceiveTransportFaultedEvent(Uri inputAddress, Exception exception, bool isTerminal)
        {
            InputAddress = inputAddress;
            Exception = exception;
            IsTerminal = isTerminal;
        }

        public Uri InputAddress { get; }

        public Exception Exception { get; }
        public bool IsTerminal { get; }
    }
}
