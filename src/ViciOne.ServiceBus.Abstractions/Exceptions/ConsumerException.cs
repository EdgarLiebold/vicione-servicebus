namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public class ConsumerException :
        ViciOneServiceBusException
    {
        public ConsumerException()
        {
        }

        public ConsumerException(string message)
            : base(message)
        {
        }

        public ConsumerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
        protected ConsumerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
