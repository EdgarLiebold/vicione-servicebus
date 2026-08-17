namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public class RoutingSlipException :
        CourierException
    {
        public RoutingSlipException()
        {
        }

        public RoutingSlipException(string message)
            : base(message)
        {
        }

        public RoutingSlipException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
        protected RoutingSlipException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
