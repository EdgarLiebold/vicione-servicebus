namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides cached access to saga connector data.</summary>
public interface ISagaConnectorCache
{
    /// <summary>Gets the connector.</summary>
    ISagaConnector Connector { get; }
}
