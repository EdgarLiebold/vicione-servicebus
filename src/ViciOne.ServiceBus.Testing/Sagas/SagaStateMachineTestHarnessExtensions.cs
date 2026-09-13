using System;
using ViciOne.ServiceBus.Testing.Internal;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Creates state-machine saga observers for a bus test harness.</summary>
public static class SagaStateMachineTestHarnessExtensions
{
    /// <summary>Registers an in-memory state-machine saga repository and records its activity.</summary>
    /// <typeparam name="TStateMachine">The state-machine implementation.</typeparam>
    /// <typeparam name="TInstance">The saga state type.</typeparam>
    /// <param name="harness">The harness that hosts the saga endpoint.</param>
    /// <param name="stateMachine">The state machine to register.</param>
    /// <param name="queueName">The dedicated endpoint queue, or <see langword="null"/> for the harness endpoint.</param>
    /// <returns>The state-machine saga harness.</returns>
    public static ISagaStateMachineTestHarness<TStateMachine, TInstance> AddSagaStateMachine<TStateMachine, TInstance>(
        this BusTestHarness harness, TStateMachine stateMachine, string? queueName = null)
        where TInstance : class, ISagaStateMachineInstance
        where TStateMachine : ISagaStateMachine<TInstance>
    {
        ArgumentNullException.ThrowIfNull(harness);
        ArgumentNullException.ThrowIfNull(stateMachine);
        var repository = new InMemorySagaRepository<TInstance>();

        return new StateMachineSagaTestHarness<TStateMachine, TInstance>(
            harness,
            repository,
            repository,
            repository,
            stateMachine,
            queueName);
    }

    /// <summary>Registers a state-machine saga repository and records the capabilities it exposes.</summary>
    /// <typeparam name="TStateMachine">The state-machine implementation.</typeparam>
    /// <typeparam name="TInstance">The saga state type.</typeparam>
    /// <param name="harness">The harness that hosts the saga endpoint.</param>
    /// <param name="stateMachine">The state machine to register.</param>
    /// <param name="repository">The saga repository to decorate.</param>
    /// <param name="queueName">The dedicated endpoint queue, or <see langword="null"/> for the harness endpoint.</param>
    /// <returns>The state-machine saga harness.</returns>
    public static ISagaStateMachineTestHarness<TStateMachine, TInstance> AddSagaStateMachine<TStateMachine, TInstance>(
        this BusTestHarness harness, TStateMachine stateMachine, ISagaRepository<TInstance> repository,
        string? queueName = null)
        where TInstance : class, ISagaStateMachineInstance
        where TStateMachine : ISagaStateMachine<TInstance>
    {
        ArgumentNullException.ThrowIfNull(harness);
        ArgumentNullException.ThrowIfNull(stateMachine);
        ArgumentNullException.ThrowIfNull(repository);

        return new StateMachineSagaTestHarness<TStateMachine, TInstance>(
            harness,
            repository,
            repository as IQuerySagaRepository<TInstance>,
            repository as ILoadSagaRepository<TInstance>,
            stateMachine,
            queueName);
    }
}
