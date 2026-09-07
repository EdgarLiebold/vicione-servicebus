using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures registration of a compensatable routing-slip activity.</summary>
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


/// <summary>Configures the execute and compensation endpoints of an activity registration.</summary>
public interface IActivityRegistrationConfigurator
{
    /// <summary>Configures the activity execution endpoint.</summary>
    /// <param name="configureExecute">The callback that configures the execution endpoint.</param>
    /// <returns>This configurator.</returns>
    IActivityRegistrationConfigurator ExecuteEndpoint(Action<IEndpointRegistrationConfigurator> configureExecute);

    /// <summary>Configures the activity compensation endpoint.</summary>
    /// <param name="configureCompensate">The callback that configures the compensation endpoint.</param>
    /// <returns>This configurator.</returns>
    IActivityRegistrationConfigurator CompensateEndpoint(Action<IEndpointRegistrationConfigurator> configureCompensate);

    /// <summary>Prevents automatic endpoint creation for this activity.</summary>
    void ExcludeFromConfigureEndpoints();
}
