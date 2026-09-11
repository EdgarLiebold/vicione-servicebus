namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides the bus configuration context used to select one message-data repository.</summary>
public interface IMessageDataRepositorySelector
{
    /// <summary>Gets the bus configurator that owns the selected repository.</summary>
    IBusFactoryConfigurator Configurator { get; }
}
