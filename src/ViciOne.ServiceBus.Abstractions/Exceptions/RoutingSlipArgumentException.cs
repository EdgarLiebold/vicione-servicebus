namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public class RoutingSlipArgumentException :
        RoutingSlipException
    {
        public RoutingSlipArgumentException()
        {
        }

        public RoutingSlipArgumentException(string message)
            : base(message)
        {
        }

        public RoutingSlipArgumentException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
        protected RoutingSlipArgumentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
