using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures saga repository registration.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface ISagaRepositoryRegistrationConfigurator<TSaga> :
    IServiceCollection
    where TSaga : class, ISaga
{
}
