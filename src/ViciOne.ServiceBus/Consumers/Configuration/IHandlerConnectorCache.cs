namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides cached access to handler connector data.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IHandlerConnectorCache<T>
    where T : class
{
    /// <summary>Gets the connector.</summary>
    IHandlerConnector<T> Connector { get; }
}
