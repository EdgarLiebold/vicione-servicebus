using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>An execute activity, which doesn't have compensation.</summary>
public interface IExecuteActivityRegistration :
    IRegistration
{
    /// <summary>Adds configure action to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    void AddConfigureAction<T, TArguments>(Action<IRegistrationContext, IExecuteActivityConfigurator<T, TArguments>>? configure)
        where T : class, IExecuteActivity<TArguments>
        where TArguments : class;

    /// <summary>Applies the supplied configuration.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context);

    /// <summary>Gets definition.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The definition.</returns>
    IExecuteActivityDefinition GetDefinition(IRegistrationContext context);
}
