namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for receive pipe configurator.
/// </summary>
public interface IReceivePipeConfigurator :
    IPipeConfigurator<ReceiveContext>
{
}
