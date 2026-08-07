// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using Microsoft.Extensions.DependencyInjection;


    public interface ISagaRepositoryRegistrationConfigurator<TSaga> :
        IServiceCollection
        where TSaga : class, ISaga
    {
    }
}
