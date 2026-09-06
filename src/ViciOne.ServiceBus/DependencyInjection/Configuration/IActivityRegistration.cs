using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>An activity, which must be configured on two separate receive endpoints.</summary>
public interface IActivityRegistration :
    IRegistration
{
    /// <summary>Adds configure action to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    void AddConfigureAction<T, TArguments>(Action<IRegistrationContext, IExecuteActivityConfigurator<T, TArguments>>? configure)
        where T : class, IExecuteActivity<TArguments>
        where TArguments : class;

    /// <summary>Adds configure action to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    void AddConfigureAction<T, TLog>(Action<IRegistrationContext, ICompensateActivityConfigurator<T, TLog>>? configure)
        where T : class, ICompensateActivity<TLog>
        where TLog : class;

    /// <summary>Applies the supplied configuration.</summary>
    /// <param name="executeEndpointConfigurator">The execute endpoint configurator.</param>
    /// <param name="compensateEndpointConfigurator">The compensate endpoint configurator.</param>
    /// <param name="context">The context associated with the operation.</param>
    void Configure(IReceiveEndpointConfigurator executeEndpointConfigurator, IReceiveEndpointConfigurator compensateEndpointConfigurator,
        IRegistrationContext context);

    /// <summary>Gets definition.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The definition.</returns>
    IActivityDefinition GetDefinition(IRegistrationContext context);

    /// <summary>Configures compensate.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    void ConfigureCompensate(IReceiveEndpointConfigurator configurator, IRegistrationContext context);

    /// <summary>Configures execute.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    void ConfigureExecute(IReceiveEndpointConfigurator configurator, IRegistrationContext context, Uri compensateAddress);
}
