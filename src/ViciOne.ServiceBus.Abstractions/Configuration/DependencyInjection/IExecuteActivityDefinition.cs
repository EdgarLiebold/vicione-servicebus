using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for execute activity definition.
/// </summary>
public interface IExecuteActivityDefinition :
    IDefinition
{
    /// <summary>
    /// The Activity type
    /// </summary>
    Type ActivityType { get; }

    /// <summary>
    /// The argument type
    /// </summary>
    Type ArgumentType { get; }

    /// <summary>
    /// Gets the execute endpoint definition value.
    /// </summary>
    IEndpointDefinition? ExecuteEndpointDefinition { get; }

    /// <summary>
    /// Return the endpoint name for the execute activity
    /// </summary>
    /// <param name="formatter"></param>
    /// <returns></returns>
    string GetExecuteEndpointName(IEndpointNameFormatter formatter);
}


/// <summary>
/// Defines the contract for execute activity definition.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public interface IExecuteActivityDefinition<TActivity, TArguments> :
    IExecuteActivityDefinition
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    /// <summary>
    /// Sets the endpoint definition, if available
    /// </summary>
    new IEndpointDefinition<IExecuteActivity<TArguments>>? ExecuteEndpointDefinition { set; }

    /// <summary>
    /// Configure the execute activity
    /// </summary>
    /// <param name="endpointConfigurator"></param>
    /// <param name="executeActivityConfigurator"></param>
    /// <param name="context"></param>
    void Configure(IReceiveEndpointConfigurator endpointConfigurator, IExecuteActivityConfigurator<TActivity, TArguments> executeActivityConfigurator,
        IRegistrationContext context);
}
