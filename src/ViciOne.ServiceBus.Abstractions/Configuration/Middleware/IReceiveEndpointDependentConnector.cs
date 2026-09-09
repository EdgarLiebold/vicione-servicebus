using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers shutdown dependents for a receive endpoint.</summary>
public interface IReceiveEndpointDependentConnector
{
    /// <summary>Adds a dependent whose completion must finish before the endpoint stops.</summary>
    /// <param name="dependent">The endpoint shutdown dependent.</param>
    void AddDependent(IReceiveEndpointDependent dependent);
}
