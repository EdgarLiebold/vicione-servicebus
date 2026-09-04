using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

public interface CachedRegistration
{
    void Register(IServiceCollection collection);
}
