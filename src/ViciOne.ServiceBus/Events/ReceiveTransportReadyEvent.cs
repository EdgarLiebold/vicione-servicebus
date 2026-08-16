namespace ViciOne.ServiceBus.Events
{
    using System;


    public class ReceiveTransportReadyEvent :
        ReceiveTransportReady
    {
        public ReceiveTransportReadyEvent(Uri inputAddress, bool isStarted = true)
        {
            InputAddress = inputAddress;
            IsStarted = isStarted;
        }

        public Uri InputAddress { get; }

        public bool IsStarted { get; }
    }
}
