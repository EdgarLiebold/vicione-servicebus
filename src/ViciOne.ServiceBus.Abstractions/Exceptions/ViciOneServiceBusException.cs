namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public class ViciOneServiceBusException :
        Exception
    {
        public ViciOneServiceBusException()
        {
        }

        public ViciOneServiceBusException(string? message)
            : base(message)
        {
        }

        public ViciOneServiceBusException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }

#if NET8_0_OR_GREATER
        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
#endif
        protected ViciOneServiceBusException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
