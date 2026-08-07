// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public class CourierException :
        ViciOneServiceBusException
    {
        public CourierException()
        {
        }

        public CourierException(string message)
            : base(message)
        {
        }

        public CourierException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

#if NET8_0_OR_GREATER
        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
#endif
        protected CourierException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
