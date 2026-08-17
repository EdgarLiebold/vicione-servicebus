namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class DuplicateKeyPipeConfigurationException :
        PipeConfigurationException
    {
        public DuplicateKeyPipeConfigurationException()
        {
        }

        public DuplicateKeyPipeConfigurationException(string message)
            : base(message)
        {
        }

        public DuplicateKeyPipeConfigurationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
