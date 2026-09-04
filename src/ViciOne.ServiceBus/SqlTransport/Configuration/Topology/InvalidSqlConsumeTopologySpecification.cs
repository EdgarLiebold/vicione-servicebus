using System.Collections.Generic;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

public class InvalidSqlConsumeTopologySpecification :
    ISqlConsumeTopologySpecification
{
    readonly string _key;
    readonly string _message;

    public InvalidSqlConsumeTopologySpecification(string key, string message)
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
