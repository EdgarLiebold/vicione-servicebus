namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class ConsumerCanceledException :
        ViciOneServiceBusException
    {
        public ConsumerCanceledException()
        {
        }

        public ConsumerCanceledException(string message)
            : base(message)
        {
        }

        public ConsumerCanceledException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
