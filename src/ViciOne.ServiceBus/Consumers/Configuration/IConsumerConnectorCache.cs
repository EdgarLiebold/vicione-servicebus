namespace ViciOne.ServiceBus.Configuration;

public interface IConsumerConnectorCache
{
    IConsumerConnector Connector { get; }
}
