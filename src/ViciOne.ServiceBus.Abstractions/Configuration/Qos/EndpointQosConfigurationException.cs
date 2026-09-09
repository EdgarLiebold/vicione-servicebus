using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Reports conflicting or invalid transport quality-of-service ownership for an endpoint.</summary>
public sealed class EndpointQosConfigurationException : ConfigurationException
{
    /// <summary>Creates an exception with the endpoint quality-of-service validation message.</summary>
    /// <param name="message">The description of the invalid endpoint configuration.</param>
    public EndpointQosConfigurationException(string message)
        : base(RequireMessage(message))
    {
    }

    static string RequireMessage(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return message;
    }
}
