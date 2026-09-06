using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by receive endpoint dependency connector.</summary>
public interface IReceiveEndpointDependencyConnector
{
    /// <summary>Add receive endpoint dependency. Endpoint will be started when dependency is Ready.</summary>
    /// <param name="dependency">The dependency.</param>
    void AddDependency(IReceiveEndpointDependency dependency);
}
