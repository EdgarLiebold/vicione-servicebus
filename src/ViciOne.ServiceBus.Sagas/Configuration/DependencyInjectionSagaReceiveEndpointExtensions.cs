using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides dependency-injection receive-endpoint extensions for sagas.</summary>
public static class DependencyInjectionSagaReceiveEndpointExtensions
{
    /// <summary>Registers a saga using the container that has the repository resolved from the container.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void Saga<T>(this IReceiveEndpointConfigurator configurator, IRegistrationContext context,
        Action<ISagaConfigurator<T>>? configure = null)
        where T : class, ISaga
    {
        ISagaRepository<T> repository = new DependencyInjectionSagaRepository<T>(context);

        configurator.Saga(repository, configure);
    }


    /// <summary>Subscribe a state machine saga to the endpoint.</summary>
    /// <typeparam name="TInstance">The state machine instance type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="stateMachine">The state machine.</param>
    /// <param name="context">The Container reference to resolve the repository.</param>
    /// <param name="configure">Optionally configure the saga.</param>
    public static void StateMachineSaga<TInstance>(this IReceiveEndpointConfigurator configurator, ISagaStateMachine<TInstance> stateMachine,
        IRegistrationContext context, Action<ISagaConfigurator<TInstance>>? configure = null)
        where TInstance : class, ISagaStateMachineInstance
    {
        ISagaRepository<TInstance> repository = new DependencyInjectionSagaRepository<TInstance>(context);

        configurator.StateMachineSaga(stateMachine, repository, configure);
    }


    /// <summary>Subscribe a state machine saga to the endpoint.</summary>
    /// <typeparam name="TInstance">The state machine instance type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The Container reference to resolve the repository.</param>
    /// <param name="configure">Optionally configure the saga.</param>
    public static void StateMachineSaga<TInstance>(this IReceiveEndpointConfigurator configurator, IRegistrationContext context,
        Action<ISagaConfigurator<TInstance>>? configure = null)
        where TInstance : class, ISagaStateMachineInstance
    {
        var stateMachine = context.GetRequiredService<ISagaStateMachine<TInstance>>();
        ISagaRepository<TInstance> repository = new DependencyInjectionSagaRepository<TInstance>(context);

        configurator.StateMachineSaga(stateMachine, repository, configure);
    }
}
