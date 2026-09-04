namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>
/// Supports connection of a consume observer
/// </summary>
public interface IConsumeObserverConnector
{
    /// <summary>
    /// Connects consume observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle ConnectConsumeObserver(IConsumeObserver observer);
}
