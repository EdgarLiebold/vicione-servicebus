using System.Collections.Generic;
using ViciOne.ServiceBus.RabbitMqTransport.Topology;

namespace ViciOne.ServiceBus.RabbitMqTransport.Configuration;

public class InvalidRabbitMqConsumeTopologySpecification :
    IRabbitMqConsumeTopologySpecification
{
    readonly string _key;
    readonly string _message;

    public InvalidRabbitMqConsumeTopologySpecification(string key, string message)
    {
        _key = key;
        _message = message;
    }

    public IEnumerable<ValidationResult> Validate()
    {
        yield return this.Failure(_key, _message);
    }

    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
    }
}
