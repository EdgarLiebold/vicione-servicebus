
namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects observers that receive consumer-configuration notifications.</summary>
public interface IConsumerConfigurationObserverConnector
{
    /// <summary>Registers a consumer-configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer);
}
