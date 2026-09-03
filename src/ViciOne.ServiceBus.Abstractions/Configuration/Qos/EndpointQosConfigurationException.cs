using System;

namespace ViciOne.ServiceBus.Configuration;

public sealed class EndpointQosConfigurationException : ConfigurationException
{
    public EndpointQosConfigurationException(string message)
        : base(message)
    {
    }
}
