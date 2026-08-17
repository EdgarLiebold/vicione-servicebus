namespace ViciOne.ServiceBus.Internals.GraphValidation
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public class CyclicGraphException :
        ViciOneServiceBusException
    {
        public CyclicGraphException()
        {
        }

        public CyclicGraphException(string message)
            : base(message)
        {
        }

        public CyclicGraphException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
        protected CyclicGraphException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
