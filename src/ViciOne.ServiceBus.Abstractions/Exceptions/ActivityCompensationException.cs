namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class ActivityCompensationException :
        CourierException
    {
        public ActivityCompensationException()
        {
        }

        public ActivityCompensationException(string message)
            : base(message)
        {
        }

        public ActivityCompensationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
