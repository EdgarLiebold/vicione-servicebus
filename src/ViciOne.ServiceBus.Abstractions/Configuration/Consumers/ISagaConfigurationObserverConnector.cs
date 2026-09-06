
namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by saga configuration observer connector.</summary>
public interface ISagaConfigurationObserverConnector
{
    /// <summary>Connects saga configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectSagaConfigurationObserver(ISagaConfigurationObserver observer);
}
