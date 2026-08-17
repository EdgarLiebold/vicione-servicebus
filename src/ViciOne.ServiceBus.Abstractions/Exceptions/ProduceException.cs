namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class ProduceException :
        ViciOneServiceBusException
    {
        public ProduceException()
        {
        }

        public ProduceException(string message)
            : base(message)
        {
        }

        public ProduceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
