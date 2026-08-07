// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    /// <summary>
    /// Use one of the selector extension methods to create a <see cref="IMessageDataRepository" /> instance for the
    /// selected repository implementation.
    /// </summary>
    public interface IMessageDataRepositorySelector
    {
        IBusFactoryConfigurator Configurator { get; }
    }
}
