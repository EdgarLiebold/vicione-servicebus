namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public class PipeFactoryException :
        ViciOneServiceBusException
    {
        public PipeFactoryException()
        {
        }

        public PipeFactoryException(string message)
            : base(message)
        {
        }

        public PipeFactoryException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
        protected PipeFactoryException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
