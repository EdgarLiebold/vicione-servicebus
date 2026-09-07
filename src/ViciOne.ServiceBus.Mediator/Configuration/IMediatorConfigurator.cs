namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures the in-process receive, send, publish, and observation pipelines.</summary>
public interface IMediatorConfigurator :
    IReceiveEndpointConfigurator,
    IConsumeObserverConnector,
    ISendObserverConnector,
    IPublishObserverConnector
{
}
