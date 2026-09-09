using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers startup prerequisites for a receive endpoint.</summary>
public interface IReceiveEndpointDependencyConnector
{
    /// <summary>Adds a prerequisite whose readiness must complete before the endpoint starts.</summary>
    /// <param name="dependency">The endpoint startup prerequisite.</param>
    void AddDependency(IReceiveEndpointDependency dependency);
}
