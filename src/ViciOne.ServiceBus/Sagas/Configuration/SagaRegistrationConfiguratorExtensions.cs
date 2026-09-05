using System;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides the saga registration surface contributed by the saga capability package.
/// </summary>
public static class SagaRegistrationConfiguratorExtensions
{
    /// <summary>
    /// Adds a saga and allows it to be configured when attached to an endpoint.
    /// </summary>
    public static ISagaRegistrationConfigurator<TSaga> AddSaga<TSaga>(this IRegistrationConfigurator configurator,
        Action<IRegistrationContext, ISagaConfigurator<TSaga>>? configure = null)
        where TSaga : class, ISaga =>
        configurator.AddSaga(null, configure);

    /// <summary>
    /// Adds a saga with an optional definition and allows it to be configured when attached to an endpoint.
    /// </summary>
    public static ISagaRegistrationConfigurator<TSaga> AddSaga<TSaga>(this IRegistrationConfigurator configurator, Type? sagaDefinitionType,
        Action<IRegistrationContext, ISagaConfigurator<TSaga>>? configure = null)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);

        if (typeof(TSaga).ImplementsInterface<SagaStateMachineInstance>())
            throw new ArgumentException($"State machine sagas must be registered using AddSagaStateMachine: {TypeCache<TSaga>.ShortName}");

        IAdvancedRegistrationConfigurator advanced = configurator.Advanced();
        ISagaRegistration registration = configurator.Services.RegisterSaga<TSaga>(advanced.Registrar, sagaDefinitionType);
        registration.AddConfigureAction(configure);
        SagaRegistrationCompletionParticipant.Ensure(configurator);

        return new SagaRegistrationConfigurator<TSaga>(configurator, registration);
    }

    /// <summary>
    /// Adds a saga state machine and allows it to be configured when attached to an endpoint.
    /// </summary>
    public static ISagaRegistrationConfigurator<TSaga> AddSagaStateMachine<TStateMachine, TSaga>(this IRegistrationConfigurator configurator,
        Action<IRegistrationContext, ISagaConfigurator<TSaga>>? configure = null)
        where TStateMachine : class, SagaStateMachine<TSaga>
        where TSaga : class, SagaStateMachineInstance =>
        configurator.AddSagaStateMachine<TStateMachine, TSaga>(null, configure);

    /// <summary>
    /// Adds a saga state machine with an optional definition and allows it to be configured when attached to an endpoint.
    /// </summary>
    public static ISagaRegistrationConfigurator<TSaga> AddSagaStateMachine<TStateMachine, TSaga>(this IRegistrationConfigurator configurator,
        Type? sagaDefinitionType, Action<IRegistrationContext, ISagaConfigurator<TSaga>>? configure = null)
        where TStateMachine : class, SagaStateMachine<TSaga>
        where TSaga : class, SagaStateMachineInstance
    {
        ArgumentNullException.ThrowIfNull(configurator);

        IAdvancedRegistrationConfigurator advanced = configurator.Advanced();
        ISagaRegistration registration = configurator.Services.RegisterSagaStateMachine<TStateMachine, TSaga>(advanced.Registrar,
            sagaDefinitionType);
        registration.AddConfigureAction(configure);
        SagaRegistrationCompletionParticipant.Ensure(configurator);

        return new SagaRegistrationConfigurator<TSaga>(configurator, registration);
    }

    /// <summary>
    /// Starts repository-only configuration for the specified saga type.
    /// </summary>
    public static ISagaRegistrationConfigurator<TSaga> AddSagaRepository<TSaga>(this IRegistrationConfigurator configurator)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        SagaRegistrationCompletionParticipant.Ensure(configurator);
        return new SagaRegistrationConfigurator<TSaga>(configurator);
    }

    /// <summary>
    /// Sets the provider that configures repositories not explicitly configured for registered sagas.
    /// </summary>
    public static void SetSagaRepositoryProvider(this IRegistrationConfigurator configurator, ISagaRepositoryRegistrationProvider provider)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(provider);
        SagaRegistrationCompletionParticipant.Ensure(configurator).Provider = provider;
    }
}
