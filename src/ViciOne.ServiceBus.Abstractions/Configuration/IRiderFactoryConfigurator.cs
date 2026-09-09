namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures an auxiliary transport hosted alongside a bus.</summary>
public interface IRiderFactoryConfigurator :
    IReceiveEndpointObserverConnector
{
}
