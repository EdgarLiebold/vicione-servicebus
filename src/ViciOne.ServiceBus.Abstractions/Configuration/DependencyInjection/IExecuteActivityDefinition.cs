using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by execute activity definition.</summary>
public interface IExecuteActivityDefinition :
    IDefinition
{
    /// <summary>The Activity type.</summary>
    Type ActivityType { get; }

    /// <summary>The argument type.</summary>
    Type ArgumentType { get; }

    /// <summary>Gets the execute endpoint definition.</summary>
    IEndpointDefinition? ExecuteEndpointDefinition { get; }

    /// <summary>Return the endpoint name for the execute activity.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The execute endpoint name.</returns>
    string GetExecuteEndpointName(IEndpointNameFormatter formatter);
}


/// <summary>Defines the operations required by execute activity definition.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public interface IExecuteActivityDefinition<TActivity, TArguments> :
    IExecuteActivityDefinition
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    /// <summary>Sets the endpoint definition, if available.</summary>
    new IEndpointDefinition<IExecuteActivity<TArguments>>? ExecuteEndpointDefinition { set; }

    /// <summary>Configure the execute activity.</summary>
    /// <param name="endpointConfigurator">The endpoint configurator.</param>
    /// <param name="executeActivityConfigurator">The execute activity configurator.</param>
    /// <param name="context">The context associated with the operation.</param>
    void Configure(IReceiveEndpointConfigurator endpointConfigurator, IExecuteActivityConfigurator<TActivity, TArguments> executeActivityConfigurator,
        IRegistrationContext context);
}
