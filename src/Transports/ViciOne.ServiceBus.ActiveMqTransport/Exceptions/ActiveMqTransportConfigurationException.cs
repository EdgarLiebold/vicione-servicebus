namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class ActiveMqTransportConfigurationException :
        ActiveMqTransportException
    {
        public ActiveMqTransportConfigurationException()
        {
        }

        public ActiveMqTransportConfigurationException(string message)
            : base(message)
        {
        }

        public ActiveMqTransportConfigurationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
