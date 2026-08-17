namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public class ConsumeContextNotAvailableException :
        ViciOneServiceBusException
    {
        public ConsumeContextNotAvailableException()
            : this("A valid ConsumeContext was not available")
        {
        }

        public ConsumeContextNotAvailableException(string message)
            : base(message)
        {
        }

        public ConsumeContextNotAvailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
        protected ConsumeContextNotAvailableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
