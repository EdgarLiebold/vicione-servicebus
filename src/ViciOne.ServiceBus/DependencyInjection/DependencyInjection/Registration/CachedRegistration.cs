// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.DependencyInjection.Registration
{
    using Microsoft.Extensions.DependencyInjection;


    public interface CachedRegistration
    {
        void Register(IServiceCollection collection);
    }
}
