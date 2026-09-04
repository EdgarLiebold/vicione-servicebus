using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>
/// Provides an activity registration configurator implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
/// <typeparam name="TLog">The t log type.</typeparam>
public class ActivityRegistrationConfigurator<TActivity, TArguments, TLog> :
    IActivityRegistrationConfigurator<TActivity, TArguments, TLog>
    where TActivity : class, IActivity<TArguments, TLog>
    where TArguments : class
    where TLog : class
{
    readonly IRegistrationConfigurator _configurator;
    readonly IActivityRegistration _registration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="registration">The registration value.</param>
    public ActivityRegistrationConfigurator(IRegistrationConfigurator configurator, IActivityRegistration registration)
    {
        _configurator = configurator;
        _registration = registration;
    }

    /// <summary>
    /// Performs the execute endpoint operation.
    /// </summary>
    /// <param name="configureExecute">The configure execute value.</param>
    /// <returns>The result of the operation.</returns>
    public IActivityRegistrationConfigurator ExecuteEndpoint(Action<IEndpointRegistrationConfigurator> configureExecute)
    {
        if (!_registration.IncludeInConfigureEndpoints)
            throw new ConfigurationException("Activity is excluded from ConfigureEndpoints");

        var configurator = new EndpointRegistrationConfigurator<IExecuteActivity<TArguments>> { ConfigureConsumeTopology = false };

        configureExecute?.Invoke(configurator);

        _configurator.AddEndpoint<ExecuteActivityEndpointDefinition<TActivity, TArguments>, IExecuteActivity<TArguments>>(_registration,
            configurator.Settings);

        return this;
    }

    /// <summary>
    /// Performs the compensate endpoint operation.
    /// </summary>
    /// <param name="configureCompensate">The configure compensate value.</param>
    /// <returns>The result of the operation.</returns>
    public IActivityRegistrationConfigurator CompensateEndpoint(Action<IEndpointRegistrationConfigurator> configureCompensate)
    {
        if (!_registration.IncludeInConfigureEndpoints)
            throw new ConfigurationException("Activity is excluded from ConfigureEndpoints");

        var compensateConfigurator = new EndpointRegistrationConfigurator<ICompensateActivity<TLog>> { ConfigureConsumeTopology = false };

        configureCompensate?.Invoke(compensateConfigurator);

        _configurator.AddEndpoint<CompensateActivityEndpointDefinition<TActivity, TLog>, ICompensateActivity<TLog>>(_registration,
            compensateConfigurator.Settings);

        return this;
    }

    /// <summary>
    /// Performs the exclude from configure endpoints operation.
    /// </summary>
    public void ExcludeFromConfigureEndpoints()
    {
        _registration.IncludeInConfigureEndpoints = false;
    }
}
