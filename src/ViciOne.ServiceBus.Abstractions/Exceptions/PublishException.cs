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

        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
        protected PublishException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
