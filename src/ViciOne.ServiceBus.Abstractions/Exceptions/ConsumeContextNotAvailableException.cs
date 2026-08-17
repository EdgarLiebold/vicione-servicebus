namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class ConsumeContextNotAvailableException :
        ViciOneServiceBusException
    {
        public ConsumeContextNotAvailableException()
            : this("A valid ConsumeContext was not available")
        {
        }

        public ConsumeContextNotAvailableException(string message)
            : base(message)
        {
        }

        public ConsumeContextNotAvailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
