using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Configures activity registration.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public class ActivityRegistrationConfigurator<TActivity, TArguments, TLog> :
    IActivityRegistrationConfigurator<TActivity, TArguments, TLog>
    where TActivity : class, IActivity<TArguments, TLog>
    where TArguments : class
    where TLog : class
{
    readonly IRegistrationConfigurator _configurator;
    readonly IActivityRegistration _registration;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="registration">The registration.</param>
    public ActivityRegistrationConfigurator(IRegistrationConfigurator configurator, IActivityRegistration registration)
    {
        _configurator = configurator;
        _registration = registration;
    }

    /// <summary>Executes endpoint.</summary>
    /// <param name="configureExecute">The configure execute.</param>
    /// <returns>The activity registration configurator produced by the operation.</returns>
    public IActivityRegistrationConfigurator ExecuteEndpoint(Action<IEndpointRegistrationConfigurator> configureExecute)
    {
        if (!_registration.IncludeInConfigureEndpoints)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Activity Registration", "unknown", "Activity is excluded from ConfigureEndpoints", "Correct the named configuration before starting the host"));

        var configurator = new EndpointRegistrationConfigurator<IExecuteActivity<TArguments>> { ConfigureConsumeTopology = false };

        configureExecute?.Invoke(configurator);

        _configurator.AddEndpoint<ExecuteActivityEndpointDefinition<TActivity, TArguments>, IExecuteActivity<TArguments>>(_registration,
            configurator.Settings);

        return this;
    }

    /// <summary>Compensates endpoint.</summary>
    /// <param name="configureCompensate">The configure compensate.</param>
    /// <returns>The activity registration configurator produced by the operation.</returns>
    public IActivityRegistrationConfigurator CompensateEndpoint(Action<IEndpointRegistrationConfigurator> configureCompensate)
    {
        if (!_registration.IncludeInConfigureEndpoints)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Activity Registration", "unknown", "Activity is excluded from ConfigureEndpoints", "Correct the named configuration before starting the host"));

        var compensateConfigurator = new EndpointRegistrationConfigurator<ICompensateActivity<TLog>> { ConfigureConsumeTopology = false };

        configureCompensate?.Invoke(compensateConfigurator);

        _configurator.AddEndpoint<CompensateActivityEndpointDefinition<TActivity, TLog>, ICompensateActivity<TLog>>(_registration,
            compensateConfigurator.Settings);

        return this;
    }

    /// <summary>Excludes from configure endpoints.</summary>
    public void ExcludeFromConfigureEndpoints()
    {
        _registration.IncludeInConfigureEndpoints = false;
    }
}
