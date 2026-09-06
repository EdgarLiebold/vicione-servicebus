using System.Collections.Generic;
using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>Represents an invalid consume-topology declaration as a deferred validation failure.</summary>
public class InvalidAmazonSqsConsumeTopologySpecification :
    IAmazonSqsConsumeTopologySpecification
{
    readonly string _key;
    readonly string _message;

    /// <summary>Initializes a deferred consume-topology validation failure.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="message">The validation message.</param>
    public InvalidAmazonSqsConsumeTopologySpecification(string key, string message)
    {
        _key = key;
        _message = message;
    }

    /// <summary>Returns the validation failure represented by this specification.</summary>
    /// <returns>A sequence containing the configured failure.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield return this.Failure(_key, _message);
    }

    /// <summary>Leaves broker topology unchanged because this specification is invalid.</summary>
    /// <param name="builder">The builder that would otherwise receive topology changes.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
    }
}
