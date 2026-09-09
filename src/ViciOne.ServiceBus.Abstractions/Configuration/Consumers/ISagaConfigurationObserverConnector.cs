
namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects observers that receive saga-configuration notifications.</summary>
public interface ISagaConfigurationObserverConnector
{
    /// <summary>Registers a saga-configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectSagaConfigurationObserver(ISagaConfigurationObserver observer);
}
