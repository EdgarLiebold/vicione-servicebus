namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides cached access to instance connector data.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IInstanceConnectorCache<T>
    where T : class
{
    /// <summary>Gets the connector.</summary>
    IInstanceConnector Connector { get; }
}
