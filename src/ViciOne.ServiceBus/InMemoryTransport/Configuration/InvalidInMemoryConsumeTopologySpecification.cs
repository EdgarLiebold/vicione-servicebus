using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Preserves a consume-topology validation failure until configuration is validated.</summary>
internal sealed class InvalidInMemoryConsumeTopologySpecification :
    IInMemoryConsumeTopologySpecification
{
    readonly string _key;
    readonly string _message;

    /// <summary>Creates a named validation failure.</summary>
    /// <param name="key">The configuration member associated with the failure.</param>
    /// <param name="message">The validation failure description.</param>
    public InvalidInMemoryConsumeTopologySpecification(string key, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        _key = key;
        _message = message;
    }

    /// <summary>Returns the preserved consume-topology failure.</summary>
    /// <returns>A sequence containing the failure.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield return this.Failure(_key, _message);
    }

    /// <summary>Leaves topology unchanged because this specification represents invalid configuration.</summary>
    /// <param name="builder">The consume topology builder that remains unchanged.</param>
    public void Apply(IMessageFabricConsumeTopologyBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
    }
}
