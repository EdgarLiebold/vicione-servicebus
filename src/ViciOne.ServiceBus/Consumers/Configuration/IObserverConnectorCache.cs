namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides cached access to observer connector data.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IObserverConnectorCache<T>
    where T : class
{
    /// <summary>Gets the connector.</summary>
    IObserverConnector<T> Connector { get; }
}
