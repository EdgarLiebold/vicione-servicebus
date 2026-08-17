namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class ConsumerException :
        ViciOneServiceBusException
    {
        public ConsumerException()
        {
        }

        public ConsumerException(string message)
            : base(message)
        {
        }

        public ConsumerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
