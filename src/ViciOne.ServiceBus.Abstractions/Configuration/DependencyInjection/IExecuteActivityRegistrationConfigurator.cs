using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures execute activity registration.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public interface IExecuteActivityRegistrationConfigurator<TActivity, TArguments> :
    IExecuteActivityRegistrationConfigurator
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
}


/// <summary>Configures execute activity registration.</summary>
public interface IExecuteActivityRegistrationConfigurator
{
    /// <summary>Applies the endpoint configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    void Endpoint(Action<IEndpointRegistrationConfigurator> configure);
    /// <summary>Excludes from configure endpoints.</summary>
    void ExcludeFromConfigureEndpoints();
}
