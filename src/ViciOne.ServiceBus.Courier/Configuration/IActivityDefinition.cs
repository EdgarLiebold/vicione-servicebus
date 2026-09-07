using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by activity definition.</summary>
public interface IActivityDefinition :
    IExecuteActivityDefinition
{
    /// <summary>Gets the activity log type.</summary>
    Type LogType { get; }

    /// <summary>Gets the compensate endpoint definition.</summary>
    IEndpointDefinition? CompensateEndpointDefinition { get; }

    /// <summary>Returns the endpoint name for the compensation activity.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The compensate endpoint name.</returns>
    string GetCompensateEndpointName(IEndpointNameFormatter formatter);
}


/// <summary>Defines the operations required by activity definition.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public interface IActivityDefinition<TActivity, TArguments, TLog> :
    IActivityDefinition,
    IExecuteActivityDefinition<TActivity, TArguments>
    where TActivity : class, IActivity<TArguments, TLog>
    where TLog : class
    where TArguments : class
{
    /// <summary>Sets the optional compensation endpoint definition.</summary>
    new IEndpointDefinition<ICompensateActivity<TLog>>? CompensateEndpointDefinition { set; }

    /// <summary>Configures the compensation activity.</summary>
    /// <param name="endpointConfigurator">The endpoint configurator.</param>
    /// <param name="compensateActivityConfigurator">The compensate activity configurator.</param>
    /// <param name="context">The context associated with the operation.</param>
    void Configure(IReceiveEndpointConfigurator endpointConfigurator, ICompensateActivityConfigurator<TActivity, TLog> compensateActivityConfigurator,
        IRegistrationContext context);
}
