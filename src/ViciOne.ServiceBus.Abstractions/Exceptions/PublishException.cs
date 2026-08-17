namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class PublishException :
        ViciOneServiceBusException
    {
        public PublishException()
        {
        }

        public PublishException(string message)
            : base(message)
        {
        }

        public PublishException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
