namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Supports connection of a consume observer.</summary>
public interface IConsumeObserverConnector
{
    /// <summary>Connects consume observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle ConnectConsumeObserver(IConsumeObserver observer);
}
