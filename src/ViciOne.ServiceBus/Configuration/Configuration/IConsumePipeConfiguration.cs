// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public interface IConsumePipeConfiguration
    {
        IConsumePipeSpecification Specification { get; }
        IConsumePipeConfigurator Configurator { get; }
    }
}
