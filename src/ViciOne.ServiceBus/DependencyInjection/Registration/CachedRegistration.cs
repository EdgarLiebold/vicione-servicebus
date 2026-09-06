using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Defines the operations required by cached registration.</summary>
public interface CachedRegistration
{
    /// <summary>Registers the supplied component.</summary>
    /// <param name="collection">The collection.</param>
    void Register(IServiceCollection collection);
}
