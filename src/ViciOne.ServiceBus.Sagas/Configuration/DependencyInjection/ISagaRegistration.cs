using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by saga registration.</summary>
public interface ISagaRegistration :
    IRegistration
{
    /// <summary>Gets the state machine type, or <see langword="null" /> for a class-based saga.</summary>
    Type? StateMachineType { get; }

    /// <summary>Adds configure action to the configuration.</summary>
    /// <typeparam name="T">The registered saga state type.</typeparam>
    /// <param name="configure">The optional callback used to configure the saga.</param>
    void AddConfigureAction<T>(Action<IRegistrationContext, ISagaConfigurator<T>>? configure)
        where T : class;

    /// <summary>Applies the supplied configuration.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context);

    /// <summary>Gets definition.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The definition.</returns>
    ISagaDefinition GetDefinition(IRegistrationContext context);
}
