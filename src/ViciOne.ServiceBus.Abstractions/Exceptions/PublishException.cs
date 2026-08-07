// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public class PublishException :
        ViciOneServiceBusException
    {
        public PublishException()
        {
        }

        public PublishException(string message)
            : base(message)
        {
        }

        public PublishException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

#if NET8_0_OR_GREATER
        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
#endif
        protected PublishException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
