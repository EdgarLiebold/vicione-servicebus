using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures registration for a specific consumer.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public interface IConsumerRegistrationConfigurator<TConsumer> :
    IConsumerRegistrationConfigurator
    where TConsumer : class, IConsumer
{
}


/// <summary>Configures endpoint ownership for a consumer registration.</summary>
public interface IConsumerRegistrationConfigurator
{
    /// <summary>Configures a dedicated receive endpoint for the consumer.</summary>
    /// <param name="configure">The callback that configures the endpoint registration.</param>
    void Endpoint(Action<IEndpointRegistrationConfigurator> configure);
    /// <summary>Excludes the consumer from automatic endpoint configuration.</summary>
    void ExcludeFromConfigureEndpoints();
}
