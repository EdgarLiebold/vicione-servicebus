namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by consume pipe specification observer connector.</summary>
public interface IConsumePipeSpecificationObserverConnector
{
    /// <summary>Connects consume pipe specification observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectConsumePipeSpecificationObserver(IConsumePipeSpecificationObserver observer);
}
