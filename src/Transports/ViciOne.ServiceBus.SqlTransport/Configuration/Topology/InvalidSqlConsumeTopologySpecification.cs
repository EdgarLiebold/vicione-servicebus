using System.Collections.Generic;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Describes requirements for invalid sql consume topology.</summary>
public class InvalidSqlConsumeTopologySpecification :
    ISqlConsumeTopologySpecification
{
    readonly string _key;
    readonly string _message;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="message">The validation failure message.</param>
    public InvalidSqlConsumeTopologySpecification(string key, string message)
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

    /// <summary>Performs no topology changes; validation reports the stored failure separately.</summary>
    /// <param name="builder">The topology builder; this specification leaves it unchanged.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
    }
}
