namespace ViciOne.ServiceBus;

public interface IMediatorConfigurator :
    IReceiveEndpointConfigurator,
    IConsumeObserverConnector,
    ISendObserverConnector,
    IPublishObserverConnector
{
}
