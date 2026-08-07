// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public interface ITransportConfiguration :
        ISpecification
    {
        ITransportConfigurator Configurator { get; }

        int PrefetchCount { get; }

        int? ConcurrentMessageLimit { get; }

        int GetConcurrentMessageLimit();
    }
}
