namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for observer connector cache.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface IObserverConnectorCache<T>
    where T : class
{
    /// <summary>
    /// Gets the connector value.
    /// </summary>
    IObserverConnector<T> Connector { get; }
}
