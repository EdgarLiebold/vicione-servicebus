using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by receive endpoint dependent connector.</summary>
public interface IReceiveEndpointDependentConnector
{
    /// <summary>Add the dependent to receive endpoint. Receive endpoint will be stopped when dependent is Completed.</summary>
    /// <param name="dependent">The dependent.</param>
    void AddDependent(IReceiveEndpointDependent dependent);
}
