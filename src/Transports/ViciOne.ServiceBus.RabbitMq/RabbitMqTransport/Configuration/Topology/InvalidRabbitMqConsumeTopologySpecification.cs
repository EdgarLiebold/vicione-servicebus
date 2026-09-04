using System.Collections.Generic;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Provides an invalid rabbit mq consume topology specification implementation.
/// </summary>
public class InvalidRabbitMqConsumeTopologySpecification :
    IRabbitMqConsumeTopologySpecification
{
    readonly string _key;
    readonly string _message;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="message">The message value.</param>
    public InvalidRabbitMqConsumeTopologySpecification(string key, string message)
    {
        _key = key;
        _message = message;
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield return this.Failure(_key, _message);
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
    }
}
