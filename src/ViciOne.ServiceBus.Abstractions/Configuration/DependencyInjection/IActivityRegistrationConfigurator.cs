using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for activity registration configurator.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
/// <typeparam name="TLog">The t log type.</typeparam>
public interface IActivityRegistrationConfigurator<TActivity, TArguments, TLog> :
    IActivityRegistrationConfigurator
    where TActivity : class, IActivity<TArguments, TLog>
    where TArguments : class
    where TLog : class
{
}


/// <summary>
/// Defines the contract for activity registration configurator.
/// </summary>
public interface IActivityRegistrationConfigurator
{
    /// <summary>
    /// Configure the activity's execute endpoint
    /// </summary>
    /// <param name="configureExecute"></param>
    IActivityRegistrationConfigurator ExecuteEndpoint(Action<IEndpointRegistrationConfigurator> configureExecute);

    /// <summary>
    /// Configure the activity's compensate endpoint
    /// </summary>
    /// <param name="configureCompensate"></param>
    IActivityRegistrationConfigurator CompensateEndpoint(Action<IEndpointRegistrationConfigurator> configureCompensate);

    /// <summary>
    /// Performs the exclude from configure endpoints operation.
    /// </summary>
    void ExcludeFromConfigureEndpoints();
}
