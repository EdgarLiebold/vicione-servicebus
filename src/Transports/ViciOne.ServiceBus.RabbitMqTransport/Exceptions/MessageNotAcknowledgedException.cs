// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


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

#if NET8_0_OR_GREATER
        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
#endif
        protected MessageNotAcknowledgedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
