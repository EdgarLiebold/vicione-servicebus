using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures consumer registration.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public interface IConsumerRegistrationConfigurator<TConsumer> :
    IConsumerRegistrationConfigurator
    where TConsumer : class, IConsumer
{
}


/// <summary>Configures consumer registration.</summary>
public interface IConsumerRegistrationConfigurator
{
    /// <summary>Applies the endpoint configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    void Endpoint(Action<IEndpointRegistrationConfigurator> configure);
    /// <summary>Excludes from configure endpoints.</summary>
    void ExcludeFromConfigureEndpoints();
}
