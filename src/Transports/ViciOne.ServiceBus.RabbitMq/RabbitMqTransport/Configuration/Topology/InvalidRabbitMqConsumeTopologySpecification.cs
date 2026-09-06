using System.Collections.Generic;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Retains a consume-topology validation failure without applying broker entities.</summary>
public class InvalidRabbitMqConsumeTopologySpecification :
    IRabbitMqConsumeTopologySpecification
{
    readonly string _key;
    readonly string _message;

    /// <summary>Creates a validation-only topology specification.</summary>
    /// <param name="key">The configuration key associated with the failure.</param>
    /// <param name="message">The validation failure description.</param>
    public InvalidRabbitMqConsumeTopologySpecification(string key, string message)
    {
        _key = key;
        _message = message;
    }

    /// <summary>Returns the retained consume-topology validation failure.</summary>
    /// <returns>A sequence containing the retained failure.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield return this.Failure(_key, _message);
    }

    /// <summary>Leaves the broker topology unchanged because this specification represents only a validation failure.</summary>
    /// <param name="builder">The receive-endpoint builder that remains unchanged.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
    }
}
