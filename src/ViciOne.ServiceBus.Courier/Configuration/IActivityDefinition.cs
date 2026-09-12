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
    /// <param name="formatter">The formatter used to derive the activity's compensation queue name.</param>
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
    /// <param name="endpointConfigurator">The receive endpoint that hosts compensation.</param>
    /// <param name="compensateActivityConfigurator">The activity-specific compensation pipeline.</param>
    /// <param name="context">The bus registration context used by definition callbacks.</param>
    void Configure(IReceiveEndpointConfigurator endpointConfigurator, ICompensateActivityConfigurator<TActivity, TLog> compensateActivityConfigurator,
        IRegistrationContext context);
}
