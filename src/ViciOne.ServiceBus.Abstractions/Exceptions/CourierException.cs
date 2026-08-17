namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class CourierException :
        ViciOneServiceBusException
    {
        public CourierException()
        {
        }

        public CourierException(string message)
            : base(message)
        {
        }

        public CourierException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
