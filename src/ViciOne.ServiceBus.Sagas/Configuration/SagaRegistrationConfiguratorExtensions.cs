using System;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides the saga registration surface contributed by the saga capability package.</summary>
public static class SagaRegistrationConfiguratorExtensions
{
    /// <summary>Adds a saga and allows it to be configured when attached to an endpoint.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The saga registration configurator produced by the operation.</returns>
    public static ISagaRegistrationConfigurator<TSaga> AddSaga<TSaga>(this IRegistrationConfigurator configurator,
        Action<IRegistrationContext, ISagaConfigurator<TSaga>>? configure = null)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);

        return configurator.AddSaga(null, configure);
    }

    /// <summary>Adds a saga with an optional definition and allows it to be configured when attached to an endpoint.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="sagaDefinitionType">The runtime saga definition type used by the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The saga registration configurator produced by the operation.</returns>
    public static ISagaRegistrationConfigurator<TSaga> AddSaga<TSaga>(this IRegistrationConfigurator configurator, Type? sagaDefinitionType,
        Action<IRegistrationContext, ISagaConfigurator<TSaga>>? configure = null)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ValidateSagaDefinitionType<TSaga>(sagaDefinitionType);

        if (typeof(TSaga).ImplementsInterface<ISagaStateMachineInstance>())
            throw new ArgumentException($"State machine sagas must be registered using AddSagaStateMachine: {TypeCache<TSaga>.ShortName}");

        IAdvancedRegistrationConfigurator advanced = configurator.Advanced();
        ISagaRegistration registration = configurator.Services.RegisterSaga<TSaga>(advanced.Registrar, sagaDefinitionType);
        registration.AddConfigureAction(configure);
        SagaRegistrationCompletionParticipant.Ensure(configurator);

        return new SagaRegistrationConfigurator<TSaga>(configurator, registration);
    }

    /// <summary>Adds a saga state machine and allows it to be configured when attached to an endpoint.</summary>
    /// <typeparam name="TStateMachine">The state machine type.</typeparam>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The saga registration configurator produced by the operation.</returns>
    public static ISagaRegistrationConfigurator<TSaga> AddSagaStateMachine<TStateMachine, TSaga>(this IRegistrationConfigurator configurator,
        Action<IRegistrationContext, ISagaConfigurator<TSaga>>? configure = null)
        where TStateMachine : class, ISagaStateMachine<TSaga>
        where TSaga : class, ISagaStateMachineInstance
    {
        ArgumentNullException.ThrowIfNull(configurator);

        return configurator.AddSagaStateMachine<TStateMachine, TSaga>(null, configure);
    }

    /// <summary>Adds a saga state machine with an optional definition and allows it to be configured when attached to an endpoint.</summary>
    /// <typeparam name="TStateMachine">The state machine type.</typeparam>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="sagaDefinitionType">The runtime saga definition type used by the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The saga registration configurator produced by the operation.</returns>
    public static ISagaRegistrationConfigurator<TSaga> AddSagaStateMachine<TStateMachine, TSaga>(this IRegistrationConfigurator configurator,
        Type? sagaDefinitionType, Action<IRegistrationContext, ISagaConfigurator<TSaga>>? configure = null)
        where TStateMachine : class, ISagaStateMachine<TSaga>
        where TSaga : class, ISagaStateMachineInstance
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ValidateSagaDefinitionType<TSaga>(sagaDefinitionType);

        IAdvancedRegistrationConfigurator advanced = configurator.Advanced();
        ISagaRegistration registration = configurator.Services.RegisterSagaStateMachine<TStateMachine, TSaga>(advanced.Registrar,
            sagaDefinitionType);
        registration.AddConfigureAction(configure);
        SagaRegistrationCompletionParticipant.Ensure(configurator);

        return new SagaRegistrationConfigurator<TSaga>(configurator, registration);
    }

    /// <summary>Starts repository-only configuration for the specified saga type.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <returns>The saga registration configurator produced by the operation.</returns>
    public static ISagaRegistrationConfigurator<TSaga> AddSagaRepository<TSaga>(this IRegistrationConfigurator configurator)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        SagaRegistrationCompletionParticipant.RequireRepository<TSaga>(configurator);
        return new SagaRegistrationConfigurator<TSaga>(configurator);
    }

    /// <summary>Sets the provider that configures repositories not explicitly configured for registered sagas.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="provider">The provider that supplies a default repository configuration.</param>
    public static void SetSagaRepositoryProvider(this IRegistrationConfigurator configurator, ISagaRepositoryRegistrationProvider provider)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(provider);
        SagaRegistrationCompletionParticipant.Ensure(configurator).Provider = provider;
    }

    static void ValidateSagaDefinitionType<TSaga>(Type? sagaDefinitionType)
        where TSaga : class, ISaga
    {
        if (sagaDefinitionType == null)
            return;

        if (!sagaDefinitionType.IsClass
            || sagaDefinitionType.IsAbstract
            || sagaDefinitionType.ContainsGenericParameters
            || !typeof(ISagaDefinition<TSaga>).IsAssignableFrom(sagaDefinitionType))
        {
            throw new ArgumentException(
                "The saga definition type must be a closed, non-abstract class compatible with the registered saga type.",
                nameof(sagaDefinitionType));
        }
    }
}
