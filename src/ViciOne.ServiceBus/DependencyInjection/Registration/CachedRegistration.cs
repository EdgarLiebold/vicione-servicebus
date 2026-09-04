using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>
/// Defines the contract for cached registration.
/// </summary>
public interface CachedRegistration
{
    /// <summary>
    /// Performs the register operation.
    /// </summary>
    /// <param name="collection">The collection value.</param>
    void Register(IServiceCollection collection);
}
