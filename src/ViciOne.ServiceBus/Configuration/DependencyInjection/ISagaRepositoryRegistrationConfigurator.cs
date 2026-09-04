using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus;

public interface ISagaRepositoryRegistrationConfigurator<TSaga> :
    IServiceCollection
    where TSaga : class, ISaga
{
}
