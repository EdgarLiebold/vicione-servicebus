using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Configures the endpoint of an execution-only activity registration.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
internal sealed class ExecuteActivityRegistrationConfigurator<TActivity, TArguments> :
    IExecuteActivityRegistrationConfigurator<TActivity, TArguments>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly IRegistrationConfigurator _configurator;
    readonly IExecuteActivityRegistration _registration;

    /// <summary>Creates the public configuration view for an execute-only activity registration.</summary>
    /// <param name="configurator">The bus registration that receives an explicit endpoint.</param>
    /// <param name="registration">The activity registration whose automatic-endpoint state is updated.</param>
    public ExecuteActivityRegistrationConfigurator(IRegistrationConfigurator configurator, IExecuteActivityRegistration registration)
    {
        _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
        _registration = registration ?? throw new ArgumentNullException(nameof(registration));
    }

    /// <summary>Configures the activity execution endpoint.</summary>
    /// <param name="configure">The callback that configures the execution endpoint.</param>
    public void Endpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        if (!_registration.IncludeInConfigureEndpoints)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Execute Activity Registration", "unknown", "ExecuteActivity is excluded from ConfigureEndpoints", "Correct the named configuration before starting the host"));

        var configurator = new EndpointRegistrationConfigurator<IExecuteActivity<TArguments>> { ConfigureConsumeTopology = false };

        configure(configurator);

        _configurator.AddEndpoint<ExecuteActivityEndpointDefinition<TActivity, TArguments>, IExecuteActivity<TArguments>>(_registration,
            configurator.Settings);
    }

    /// <summary>Prevents automatic endpoint creation for this activity.</summary>
    public void ExcludeFromConfigureEndpoints()
    {
        _registration.IncludeInConfigureEndpoints = false;
    }
}
