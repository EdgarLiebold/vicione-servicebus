using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Coordinates a compensatable activity across its execution and compensation endpoints.</summary>
internal interface IActivityRegistration :
    IRegistration
{
    /// <summary>Adds an execution-pipeline configuration action for a matching activity signature.</summary>
    /// <typeparam name="T">The activity type.</typeparam>
    /// <typeparam name="TArguments">The activity argument type.</typeparam>
    /// <param name="configure">The execution-pipeline configuration action.</param>
    void AddConfigureAction<T, TArguments>(Action<IRegistrationContext, IExecuteActivityConfigurator<T, TArguments>>? configure)
        where T : class, IExecuteActivity<TArguments>
        where TArguments : class;

    /// <summary>Adds a compensation-pipeline configuration action for a matching activity signature.</summary>
    /// <typeparam name="T">The activity type.</typeparam>
    /// <typeparam name="TLog">The activity log type.</typeparam>
    /// <param name="configure">The compensation-pipeline configuration action.</param>
    void AddConfigureAction<T, TLog>(Action<IRegistrationContext, ICompensateActivityConfigurator<T, TLog>>? configure)
        where T : class, ICompensateActivity<TLog>
        where TLog : class;

    /// <summary>Configures the paired execution and compensation endpoints.</summary>
    /// <param name="executeEndpointConfigurator">The execute endpoint configurator.</param>
    /// <param name="compensateEndpointConfigurator">The compensate endpoint configurator.</param>
    /// <param name="context">The context associated with the operation.</param>
    void Configure(IReceiveEndpointConfigurator executeEndpointConfigurator, IReceiveEndpointConfigurator compensateEndpointConfigurator,
        IRegistrationContext context);

    /// <summary>Gets the resolved activity definition.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The definition.</returns>
    IActivityDefinition GetDefinition(IRegistrationContext context);

    /// <summary>Configures the compensation endpoint.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    void ConfigureCompensate(IReceiveEndpointConfigurator configurator, IRegistrationContext context);

    /// <summary>Configures the execution endpoint.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    void ConfigureExecute(IReceiveEndpointConfigurator configurator, IRegistrationContext context, Uri compensateAddress);
}
