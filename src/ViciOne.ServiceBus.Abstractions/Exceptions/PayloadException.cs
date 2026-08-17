namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class PayloadException :
        ViciOneServiceBusException
    {
        public PayloadException()
        {
        }

        public PayloadException(string message)
            : base(message)
        {
        }

        public PayloadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
