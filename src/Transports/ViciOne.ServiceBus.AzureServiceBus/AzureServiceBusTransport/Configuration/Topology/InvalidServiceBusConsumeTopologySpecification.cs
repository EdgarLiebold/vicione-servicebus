using System.Collections.Generic;
using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Preserves a consume-topology configuration failure so validation can report it with the endpoint.</summary>
public class InvalidServiceBusConsumeTopologySpecification :
    IServiceBusConsumeTopologySpecification
{
    readonly string _key;
    readonly string _message;

    /// <summary>Initializes a deferred validation failure.</summary>
    /// <param name="key">The configuration key associated with the failure.</param>
    /// <param name="message">The validation message.</param>
    public InvalidServiceBusConsumeTopologySpecification(string key, string message)
    {
        _key = key;
        _message = message;
    }

    /// <summary>Returns the deferred configuration failure.</summary>
    /// <returns>A sequence containing the configured failure.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield return this.Failure(_key, _message);
    }

    /// <summary>Performs no topology mutation because this specification represents an invalid configuration.</summary>
    /// <param name="builder">The builder intentionally left unchanged.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
    }
}
