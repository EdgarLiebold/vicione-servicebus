namespace ViciOne.ServiceBus
{
    using System;


    /// <summary>
    /// Thrown when a message is not acknowledged by the broker
    /// </summary>
    [Serializable]
    public class MessageNotAcknowledgedException :
        TransportException
    {
        public MessageNotAcknowledgedException()
        {
        }

        public MessageNotAcknowledgedException(Uri uri, string message)
            : base(uri, message)
        {
        }
    }
}
