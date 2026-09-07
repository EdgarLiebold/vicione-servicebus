using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures saga registration.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface ISagaRegistrationConfigurator<TSaga> :
    ISagaRegistrationConfigurator
    where TSaga : class, ISaga
{
    /// <summary>Applies the endpoint configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The saga registration configurator produced by the operation.</returns>
    new ISagaRegistrationConfigurator<TSaga> Endpoint(Action<IEndpointRegistrationConfigurator> configure);
    /// <summary>Applies the repository configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The saga registration configurator produced by the operation.</returns>
    ISagaRegistrationConfigurator<TSaga> Repository(Action<ISagaRepositoryRegistrationConfigurator<TSaga>> configure);
}


/// <summary>Configures saga registration.</summary>
public interface ISagaRegistrationConfigurator
{
    /// <summary>Applies the endpoint configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The saga registration configurator produced by the operation.</returns>
    ISagaRegistrationConfigurator Endpoint(Action<IEndpointRegistrationConfigurator> configure);
    /// <summary>Excludes from configure endpoints.</summary>
    void ExcludeFromConfigureEndpoints();
}
