namespace ViciOne.ServiceBus
{
    using System;
    using System.Collections.Generic;


    [Serializable]
    public class ConfigurationException :
        ViciOneServiceBusException
    {
        public ConfigurationException()
        {
        }

        public ConfigurationException(IEnumerable<ValidationResult> results, string message)
            : base(message)
        {
            Results = results;
        }

        public ConfigurationException(IEnumerable<ValidationResult> results, string message, Exception innerException)
            : base(message, innerException)
        {
            Results = results;
        }

        public ConfigurationException(string message)
            : base(message)
        {
        }

        public ConfigurationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public IEnumerable<ValidationResult> Results { get; protected set; } = [];
    }
}
