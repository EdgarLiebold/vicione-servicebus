using System.Collections.Generic;
using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Represents a rejected consume-topology request as a validation failure.</summary>
public class InvalidActiveMqConsumeTopologySpecification :
    IActiveMqConsumeTopologySpecification
{
    readonly string _key;
    readonly string _message;

    /// <summary>Creates a validation-only topology specification.</summary>
    /// <param name="key">The validation key.</param>
    /// <param name="message">The validation failure message.</param>
    public InvalidActiveMqConsumeTopologySpecification(string key, string message)
    {
        _key = key;
        _message = message;
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield return this.Failure(_key, _message);
    }

    /// <summary>Performs no topology mutation because the specification is invalid.</summary>
    /// <param name="builder">The receive-topology builder, intentionally left unchanged.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
    }
}
