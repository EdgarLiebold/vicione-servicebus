
namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects observers that receive handler-configuration notifications.</summary>
public interface IHandlerConfigurationObserverConnector
{
    /// <summary>Registers a handler-configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectHandlerConfigurationObserver(IHandlerConfigurationObserver observer);
}
