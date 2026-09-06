
namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by handler configuration observer connector.</summary>
public interface IHandlerConfigurationObserverConnector
{
    /// <summary>Connects handler configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectHandlerConfigurationObserver(IHandlerConfigurationObserver observer);
}
