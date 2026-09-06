using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Represents an error related to endpoint qos configuration.</summary>
public sealed class EndpointQosConfigurationException : ConfigurationException
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public EndpointQosConfigurationException(string message)
        : base(message)
    {
    }
}
