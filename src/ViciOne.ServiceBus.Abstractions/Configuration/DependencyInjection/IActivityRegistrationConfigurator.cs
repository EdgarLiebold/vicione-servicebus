using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures activity registration.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public interface IActivityRegistrationConfigurator<TActivity, TArguments, TLog> :
    IActivityRegistrationConfigurator
    where TActivity : class, IActivity<TArguments, TLog>
    where TArguments : class
    where TLog : class
{
}


/// <summary>Configures activity registration.</summary>
public interface IActivityRegistrationConfigurator
{
    /// <summary>Configure the activity's execute endpoint.</summary>
    /// <param name="configureExecute">The configure execute.</param>
    /// <returns>The activity registration configurator produced by the operation.</returns>
    IActivityRegistrationConfigurator ExecuteEndpoint(Action<IEndpointRegistrationConfigurator> configureExecute);

    /// <summary>Configure the activity's compensate endpoint.</summary>
    /// <param name="configureCompensate">The configure compensate.</param>
    /// <returns>The activity registration configurator produced by the operation.</returns>
    IActivityRegistrationConfigurator CompensateEndpoint(Action<IEndpointRegistrationConfigurator> configureCompensate);

    /// <summary>Excludes from configure endpoints.</summary>
    void ExcludeFromConfigureEndpoints();
}
