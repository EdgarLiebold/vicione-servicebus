namespace ViciOne.ServiceBus.DependencyInjection.Registration
{
    using Microsoft.Extensions.DependencyInjection;


    public interface CachedRegistration
    {
        void Register(IServiceCollection collection);
    }
}
