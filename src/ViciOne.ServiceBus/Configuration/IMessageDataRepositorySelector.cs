namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Use one of the selector extension methods to create a <see cref="IMessageDataRepository" /> instance for the
/// selected repository implementation.
/// </summary>
public interface IMessageDataRepositorySelector
{
    /// <summary>Gets the configurator.</summary>
    IBusFactoryConfigurator Configurator { get; }
}
