using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// An activity, which must be configured on two separate receive endpoints
/// </summary>
public interface IActivityRegistration :
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
    /// Adds configure action to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    void AddConfigureAction<T, TLog>(Action<IRegistrationContext, ICompensateActivityConfigurator<T, TLog>>? configure)
        where T : class, ICompensateActivity<TLog>
        where TLog : class;

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="executeEndpointConfigurator">The execute endpoint configurator value.</param>
    /// <param name="compensateEndpointConfigurator">The compensate endpoint configurator value.</param>
    /// <param name="context">The operation context.</param>
    void Configure(IReceiveEndpointConfigurator executeEndpointConfigurator, IReceiveEndpointConfigurator compensateEndpointConfigurator,
        IRegistrationContext context);

    /// <summary>
    /// Gets definition.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    IActivityDefinition GetDefinition(IRegistrationContext context);

    /// <summary>
    /// Configures compensate.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="context">The operation context.</param>
    void ConfigureCompensate(IReceiveEndpointConfigurator configurator, IRegistrationContext context);

    /// <summary>
    /// Configures execute.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="compensateAddress">The compensate address value.</param>
    void ConfigureExecute(IReceiveEndpointConfigurator configurator, IRegistrationContext context, Uri compensateAddress);
}
