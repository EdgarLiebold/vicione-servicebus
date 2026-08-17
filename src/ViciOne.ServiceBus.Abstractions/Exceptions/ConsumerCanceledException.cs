namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public class ConsumerCanceledException :
        ViciOneServiceBusException
    {
        public ConsumerCanceledException()
        {
        }

        public ConsumerCanceledException(string message)
            : base(message)
        {
        }

        public ConsumerCanceledException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
        protected ConsumerCanceledException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
