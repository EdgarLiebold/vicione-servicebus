using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures registration of an execution-only routing-slip activity.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public interface IExecuteActivityRegistrationConfigurator<TActivity, TArguments> :
    IExecuteActivityRegistrationConfigurator
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
}


/// <summary>Configures an execution-only activity registration.</summary>
public interface IExecuteActivityRegistrationConfigurator
{
    /// <summary>Configures the activity execution endpoint.</summary>
    /// <param name="configure">The callback that configures the execution endpoint.</param>
    void Endpoint(Action<IEndpointRegistrationConfigurator> configure);
    /// <summary>Prevents automatic endpoint creation for this activity.</summary>
    void ExcludeFromConfigureEndpoints();
}
