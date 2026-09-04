namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>
/// Defines the contract for receive observer connector.
/// </summary>
public interface IReceiveObserverConnector
{
    /// <summary>
    /// Connect an observer to the receiving endpoint
    /// </summary>
    /// <param name="observer"></param>
    /// <returns></returns>
    ConnectHandle ConnectReceiveObserver(IReceiveObserver observer);
}
