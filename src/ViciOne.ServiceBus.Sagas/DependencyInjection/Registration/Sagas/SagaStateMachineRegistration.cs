using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>
/// A saga state machine represents a state machine and instance, which will use the container to resolve, as well
/// as the saga repository.
/// </summary>
/// <typeparam name="TStateMachine">The state machine type.</typeparam>
/// <typeparam name="TInstance">The instance type.</typeparam>
public class SagaStateMachineRegistration<TStateMachine, TInstance> :
    ISagaRegistration
    where TStateMachine : class, ISagaStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    readonly IContainerSelector _selector;
    readonly List<Action<IRegistrationContext, ISagaConfigurator<TInstance>>> _configureActions;
    ISagaDefinition<TInstance> _definition = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="selector">The selector.</param>
    public SagaStateMachineRegistration(IContainerSelector selector)
    {
        _selector = selector;
        _configureActions = new List<Action<IRegistrationContext, ISagaConfigurator<TInstance>>>();
        IncludeInConfigureEndpoints = !Type.HasAttribute<ExcludeFromConfigureEndpointsAttribute>();
    }

    /// <summary>Gets the type.</summary>
    public Type Type => typeof(TInstance);

    /// <inheritdoc />
    public Type? StateMachineType => typeof(TStateMachine);

    /// <summary>Gets or sets the include in configure endpoints.</summary>
    public bool IncludeInConfigureEndpoints { get; set; }

    /// <summary>Adds configure action to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    public void AddConfigureAction<T>(Action<IRegistrationContext, ISagaConfigurator<T>>? configure)
        where T : class
    {
        if (configure is Action<IRegistrationContext, ISagaConfigurator<TInstance>> action)
            _configureActions.Add(action);
    }

    /// <summary>Applies the supplied configuration.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    public void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context)
    {
        var stateMachine = context.GetRequiredService<ISagaStateMachine<TInstance>>();
        ISagaRepository<TInstance> repository = new DependencyInjectionSagaRepository<TInstance>(context);

        var decoratorRegistration = context.GetService<ISagaRepositoryDecoratorRegistration<TInstance>>();
        if (decoratorRegistration != null)
            repository = decoratorRegistration.DecorateSagaRepository(repository);

        var stateMachineConfigurator = new ViciOneServiceBusStateMachine<TInstance>.StateMachineSagaConfigurator(stateMachine, repository, configurator);

        GetSagaDefinition(context)
            .Configure(configurator, stateMachineConfigurator, context);

        foreach (Action<IRegistrationContext, ISagaConfigurator<TInstance>> action in _configureActions)
            action(context, stateMachineConfigurator);

        IEnumerable<IEventObserver<TInstance>> eventObservers = context.GetServices<IEventObserver<TInstance>>();
        foreach (IEventObserver<TInstance> eventObserver in eventObservers)
            stateMachine.ConnectEventObserver(eventObserver);

        IEnumerable<IStateObserver<TInstance>> stateObservers = context.GetServices<IStateObserver<TInstance>>();
        foreach (IStateObserver<TInstance> stateObserver in stateObservers)
            stateMachine.ConnectStateObserver(stateObserver);

        LogContext.Info?.Log("Configured endpoint {Endpoint}, Saga: {SagaType}, State Machine: {StateMachineType}",
            configurator.InputAddress.GetEndpointName(),
            TypeCache<TInstance>.ShortName, TypeCache.GetShortName(stateMachine.GetType()));

        configurator.AddEndpointSpecification(stateMachineConfigurator);
    }

    ISagaDefinition ISagaRegistration.GetDefinition(IRegistrationContext context)
    {
        return GetSagaDefinition(context);
    }

    ISagaDefinition<TInstance> GetSagaDefinition(IServiceProvider provider)
    {
        if (_definition != null)
            return _definition;

        _definition = _selector.GetDefinition<ISagaDefinition<TInstance>>(provider) ?? new DefaultSagaDefinition<TInstance>();

        IEndpointDefinition<TInstance>? endpointDefinition = _selector.GetEndpointDefinition<TInstance>(provider);
        if (endpointDefinition != null)
            _definition.EndpointDefinition = endpointDefinition;

        return _definition;
    }
}
