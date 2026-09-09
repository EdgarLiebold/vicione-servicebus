namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures middleware that processes transport receive contexts before deserialization.</summary>
public interface IReceivePipeConfigurator :
    IPipeConfigurator<ReceiveContext>
{
}
