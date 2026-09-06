namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by send pipe specification observer connector.</summary>
public interface ISendPipeSpecificationObserverConnector
{
    /// <summary>Connects send pipe specification observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectSendPipeSpecificationObserver(ISendPipeSpecificationObserver observer);
}
