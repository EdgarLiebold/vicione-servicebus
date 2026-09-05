using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides dependency-injection receive-endpoint extensions for sagas.
/// </summary>
public static class DependencyInjectionSagaReceiveEndpointExtensions
{
    /// <summary>
    /// Registers a saga using the container that has the repository resolved from the container
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="configurator"></param>
    /// <param name="context"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static void Saga<T>(this IReceiveEndpointConfigurator configurator, IRegistrationContext context,
        Action<ISagaConfigurator<T>>? configure = null)
        where T : class, ISaga
    {
        ISagaRepository<T> repository = new DependencyInjectionSagaRepository<T>(context);

        configurator.Saga(repository, configure);
    }


    /// <summary>
    /// Subscribe a state machine saga to the endpoint
    /// </summary>
    /// <typeparam name="TInstance">The state machine instance type</typeparam>
    /// <param name="configurator"></param>
    /// <param name="stateMachine"></param>
    /// <param name="context">The Container reference to resolve the repository</param>
    /// <param name="configure">Optionally configure the saga</param>
    /// <returns></returns>
    public static void StateMachineSaga<TInstance>(this IReceiveEndpointConfigurator configurator, SagaStateMachine<TInstance> stateMachine,
        IRegistrationContext context, Action<ISagaConfigurator<TInstance>>? configure = null)
        where TInstance : class, SagaStateMachineInstance
    {
        ISagaRepository<TInstance> repository = new DependencyInjectionSagaRepository<TInstance>(context);

        configurator.StateMachineSaga(stateMachine, repository, configure);
    }


    /// <summary>
    /// Subscribe a state machine saga to the endpoint
    /// </summary>
    /// <typeparam name="TInstance">The state machine instance type</typeparam>
    /// <param name="configurator"></param>
    /// <param name="context">The Container reference to resolve the repository</param>
    /// <param name="configure">Optionally configure the saga</param>
    /// <returns></returns>
    public static void StateMachineSaga<TInstance>(this IReceiveEndpointConfigurator configurator, IRegistrationContext context,
        Action<ISagaConfigurator<TInstance>>? configure = null)
        where TInstance : class, SagaStateMachineInstance
    {
        var stateMachine = context.GetRequiredService<SagaStateMachine<TInstance>>();
        ISagaRepository<TInstance> repository = new DependencyInjectionSagaRepository<TInstance>(context);

        configurator.StateMachineSaga(stateMachine, repository, configure);
    }
}
