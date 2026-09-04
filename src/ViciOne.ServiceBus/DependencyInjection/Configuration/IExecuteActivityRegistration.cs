using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// An execute activity, which doesn't have compensation
/// </summary>
public interface IExecuteActivityRegistration :
    IRegistration
{
    /// <summary>
    /// Adds configure action to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    void AddConfigureAction<T, TArguments>(Action<IRegistrationContext, IExecuteActivityConfigurator<T, TArguments>>? configure)
        where T : class, IExecuteActivity<TArguments>
        where TArguments : class;

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="context">The operation context.</param>
    void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context);

    /// <summary>
    /// Gets definition.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    IExecuteActivityDefinition GetDefinition(IRegistrationContext context);
}
