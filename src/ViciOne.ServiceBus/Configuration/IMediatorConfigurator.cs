namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for mediator configurator.
/// </summary>
public interface IMediatorConfigurator :
    IReceiveEndpointConfigurator,
    IConsumeObserverConnector,
    ISendObserverConnector,
    IPublishObserverConnector
{
}
