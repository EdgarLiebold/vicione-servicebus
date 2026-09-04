using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>
/// Provides an execute activity registration configurator implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public class ExecuteActivityRegistrationConfigurator<TActivity, TArguments> :
    IExecuteActivityRegistrationConfigurator<TActivity, TArguments>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly IRegistrationConfigurator _configurator;
    readonly IExecuteActivityRegistration _registration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="registration">The registration value.</param>
    public ExecuteActivityRegistrationConfigurator(IRegistrationConfigurator configurator, IExecuteActivityRegistration registration)
    {
        _configurator = configurator;
        _registration = registration;
    }

    /// <summary>
    /// Performs the endpoint operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void Endpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        if (!_registration.IncludeInConfigureEndpoints)
            throw new ConfigurationException("ExecuteActivity is excluded from ConfigureEndpoints");

        var configurator = new EndpointRegistrationConfigurator<IExecuteActivity<TArguments>> { ConfigureConsumeTopology = false };

        configure?.Invoke(configurator);

        _configurator.AddEndpoint<ExecuteActivityEndpointDefinition<TActivity, TArguments>, IExecuteActivity<TArguments>>(_registration,
            configurator.Settings);
    }

    /// <summary>
    /// Performs the exclude from configure endpoints operation.
    /// </summary>
    public void ExcludeFromConfigureEndpoints()
    {
        _registration.IncludeInConfigureEndpoints = false;
    }
}
