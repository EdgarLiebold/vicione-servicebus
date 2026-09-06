namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures mediator.</summary>
public interface IMediatorConfigurator :
    IReceiveEndpointConfigurator,
    IConsumeObserverConnector,
    ISendObserverConnector,
    IPublishObserverConnector
{
}
