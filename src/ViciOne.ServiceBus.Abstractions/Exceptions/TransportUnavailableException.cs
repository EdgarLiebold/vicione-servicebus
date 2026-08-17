namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public class TransportUnavailableException :
        ViciOneServiceBusException
    {
        public TransportUnavailableException()
        {
        }

        public TransportUnavailableException(string message)
            : base(message)
        {
        }

        public TransportUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
        protected TransportUnavailableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
