
namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by consumer configuration observer connector.</summary>
public interface IConsumerConfigurationObserverConnector
{
    /// <summary>Connects consumer configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer);
}
