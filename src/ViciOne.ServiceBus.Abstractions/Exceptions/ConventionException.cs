namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public class ConventionException :
        ViciOneServiceBusException
    {
        public ConventionException()
        {
        }

        public ConventionException(string message)
            : base(message)
        {
        }

        public ConventionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
        protected ConventionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
