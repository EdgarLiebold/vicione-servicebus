using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Configures the endpoints of a compensatable activity registration.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
internal sealed class ActivityRegistrationConfigurator<TActivity, TArguments, TLog> :
    IActivityRegistrationConfigurator<TActivity, TArguments, TLog>
    where TActivity : class, IActivity<TArguments, TLog>
    where TArguments : class
    where TLog : class
{
    readonly IRegistrationConfigurator _configurator;
    readonly IActivityRegistration _registration;

    /// <summary>Creates the public configuration view for a compensatable activity registration.</summary>
    /// <param name="configurator">The bus registration that receives explicit endpoints.</param>
    /// <param name="registration">The activity registration whose automatic-endpoint state is updated.</param>
    public ActivityRegistrationConfigurator(IRegistrationConfigurator configurator, IActivityRegistration registration)
    {
        _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
        _registration = registration ?? throw new ArgumentNullException(nameof(registration));
    }

    /// <summary>Configures the activity execution endpoint.</summary>
    /// <param name="configureExecute">The callback that configures the execution endpoint.</param>
    /// <returns>This configurator.</returns>
    public IActivityRegistrationConfigurator ExecuteEndpoint(Action<IEndpointRegistrationConfigurator> configureExecute)
    {
        ArgumentNullException.ThrowIfNull(configureExecute);

        if (!_registration.IncludeInConfigureEndpoints)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Activity Registration", "unknown", "Activity is excluded from ConfigureEndpoints", "Correct the named configuration before starting the host"));

        var configurator = new EndpointRegistrationConfigurator<IExecuteActivity<TArguments>> { ConfigureConsumeTopology = false };

        configureExecute(configurator);

        _configurator.AddEndpoint<ExecuteActivityEndpointDefinition<TActivity, TArguments>, IExecuteActivity<TArguments>>(_registration,
            configurator.Settings);

        return this;
    }

    /// <summary>Configures the activity compensation endpoint.</summary>
    /// <param name="configureCompensate">The callback that configures the compensation endpoint.</param>
    /// <returns>This configurator.</returns>
    public IActivityRegistrationConfigurator CompensateEndpoint(Action<IEndpointRegistrationConfigurator> configureCompensate)
    {
        ArgumentNullException.ThrowIfNull(configureCompensate);

        if (!_registration.IncludeInConfigureEndpoints)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Activity Registration", "unknown", "Activity is excluded from ConfigureEndpoints", "Correct the named configuration before starting the host"));

        var compensateConfigurator = new EndpointRegistrationConfigurator<ICompensateActivity<TLog>> { ConfigureConsumeTopology = false };

        configureCompensate(compensateConfigurator);

        _configurator.AddEndpoint<CompensateActivityEndpointDefinition<TActivity, TLog>, ICompensateActivity<TLog>>(_registration,
            compensateConfigurator.Settings);

        return this;
    }

    /// <summary>Prevents automatic endpoint creation for this activity.</summary>
    public void ExcludeFromConfigureEndpoints()
    {
        _registration.IncludeInConfigureEndpoints = false;
    }
}
