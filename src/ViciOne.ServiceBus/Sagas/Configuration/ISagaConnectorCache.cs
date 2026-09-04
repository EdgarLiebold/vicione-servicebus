namespace ViciOne.ServiceBus.Configuration;

public interface ISagaConnectorCache
{
    ISagaConnector Connector { get; }
}
