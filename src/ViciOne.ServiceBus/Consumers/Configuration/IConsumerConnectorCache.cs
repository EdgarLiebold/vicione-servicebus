namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides cached access to consumer connector data.</summary>
public interface IConsumerConnectorCache
{
    /// <summary>Gets the connector.</summary>
    IConsumerConnector Connector { get; }
}
