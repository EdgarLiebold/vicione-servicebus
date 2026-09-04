using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for activity definition.
/// </summary>
public interface IActivityDefinition :
    IExecuteActivityDefinition
{
    /// <summary>
    /// The log type
    /// </summary>
    Type LogType { get; }

    /// <summary>
    /// Gets the compensate endpoint definition value.
    /// </summary>
    IEndpointDefinition? CompensateEndpointDefinition { get; }

    /// <summary>
    /// Return the endpoint name for the compensate activity
    /// </summary>
    /// <param name="formatter"></param>
    /// <returns></returns>
    string GetCompensateEndpointName(IEndpointNameFormatter formatter);
}


/// <summary>
/// Defines the contract for activity definition.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
/// <typeparam name="TLog">The t log type.</typeparam>
public interface IActivityDefinition<TActivity, TArguments, TLog> :
    IActivityDefinition,
    IExecuteActivityDefinition<TActivity, TArguments>
    where TActivity : class, IActivity<TArguments, TLog>
    where TLog : class
    where TArguments : class
{
    /// <summary>
    /// Sets the endpoint definition, if available
    /// </summary>
    new IEndpointDefinition<ICompensateActivity<TLog>> CompensateEndpointDefinition { set; }

    /// <summary>
    /// Configure the compensate activity
    /// </summary>
    /// <param name="endpointConfigurator"></param>
    /// <param name="compensateActivityConfigurator"></param>
    /// <param name="context"></param>
    void Configure(IReceiveEndpointConfigurator endpointConfigurator, ICompensateActivityConfigurator<TActivity, TLog> compensateActivityConfigurator,
        IRegistrationContext context);
}
