using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for saga repository registration configurator.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface ISagaRepositoryRegistrationConfigurator<TSaga> :
    IServiceCollection
    where TSaga : class, ISaga
{
}
