namespace ViciOne.ServiceBus
{
    using System;


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
    }
}
