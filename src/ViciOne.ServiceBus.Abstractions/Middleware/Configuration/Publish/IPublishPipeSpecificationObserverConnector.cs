namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by publish pipe specification observer connector.</summary>
public interface IPublishPipeSpecificationObserverConnector
{
    /// <summary>Connects publish pipe specification observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectPublishPipeSpecificationObserver(IPublishPipeSpecificationObserver observer);
}
