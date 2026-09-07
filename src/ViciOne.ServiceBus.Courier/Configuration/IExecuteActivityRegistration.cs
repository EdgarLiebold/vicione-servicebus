using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Coordinates registration of an execution-only activity.</summary>
internal interface IExecuteActivityRegistration :
    IRegistration
{
    /// <summary>Adds an execution-pipeline configuration action for a matching activity signature.</summary>
    /// <typeparam name="T">The activity type.</typeparam>
    /// <typeparam name="TArguments">The activity argument type.</typeparam>
    /// <param name="configure">The execution-pipeline configuration action.</param>
    void AddConfigureAction<T, TArguments>(Action<IRegistrationContext, IExecuteActivityConfigurator<T, TArguments>>? configure)
        where T : class, IExecuteActivity<TArguments>
        where TArguments : class;

    /// <summary>Configures the activity execution endpoint.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context);

    /// <summary>Gets the resolved activity definition.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The definition.</returns>
    IExecuteActivityDefinition GetDefinition(IRegistrationContext context);
}
