namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class PipeConfigurationException :
        ViciOneServiceBusException
    {
        public PipeConfigurationException()
        {
        }

        public PipeConfigurationException(string message)
            : base(message)
        {
        }

        public PipeConfigurationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
