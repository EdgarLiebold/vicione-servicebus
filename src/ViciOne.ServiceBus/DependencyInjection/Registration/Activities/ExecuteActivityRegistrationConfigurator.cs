using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Configures execute activity registration.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public class ExecuteActivityRegistrationConfigurator<TActivity, TArguments> :
    IExecuteActivityRegistrationConfigurator<TActivity, TArguments>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly IRegistrationConfigurator _configurator;
    readonly IExecuteActivityRegistration _registration;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="registration">The registration.</param>
    public ExecuteActivityRegistrationConfigurator(IRegistrationConfigurator configurator, IExecuteActivityRegistration registration)
    {
        _configurator = configurator;
        _registration = registration;
    }

    /// <summary>Applies the endpoint configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public void Endpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        if (!_registration.IncludeInConfigureEndpoints)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Execute Activity Registration", "unknown", "ExecuteActivity is excluded from ConfigureEndpoints", "Correct the named configuration before starting the host"));

        var configurator = new EndpointRegistrationConfigurator<IExecuteActivity<TArguments>> { ConfigureConsumeTopology = false };

        configure?.Invoke(configurator);

        _configurator.AddEndpoint<ExecuteActivityEndpointDefinition<TActivity, TArguments>, IExecuteActivity<TArguments>>(_registration,
            configurator.Settings);
    }

    /// <summary>Excludes from configure endpoints.</summary>
    public void ExcludeFromConfigureEndpoints()
    {
        _registration.IncludeInConfigureEndpoints = false;
    }
}
