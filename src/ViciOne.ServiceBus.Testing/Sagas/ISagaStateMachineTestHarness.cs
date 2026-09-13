using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Exposes repository, event, and state-transition observations for a state-machine saga.</summary>
/// <typeparam name="TStateMachine">The state-machine implementation.</typeparam>
/// <typeparam name="TInstance">The saga state type.</typeparam>
public interface ISagaStateMachineTestHarness<out TStateMachine, TInstance> :
    ISagaTestHarness<TInstance>,
    IDisposable
    where TStateMachine : ISagaStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>Gets the observed state machine.</summary>
    TStateMachine StateMachine { get; }

    /// <summary>Gets retained event execution observations.</summary>
    IReadOnlyList<StateMachineEventObservation> Events { get; }

    /// <summary>Gets retained state-transition observations.</summary>
    IReadOnlyList<StateMachineStateChange> StateChanges { get; }

    /// <summary>Waits until the identified saga reaches a state selected from the state machine.</summary>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="stateSelector">The selector for the expected state.</param>
    /// <param name="timeout">The polling timeout, or <see langword="null"/> to use the harness default.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier when the saga reaches the state; otherwise, <see langword="null"/>.</returns>
    Task<Guid?> WaitForSagaInStateAsync(Guid correlationId, Func<TStateMachine, IState> stateSelector,
        TimeSpan? timeout = default, CancellationToken cancellationToken = default);

    /// <summary>Waits until the identified saga reaches a specified state.</summary>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="state">The expected state.</param>
    /// <param name="timeout">The polling timeout, or <see langword="null"/> to use the harness default.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier when the saga reaches the state; otherwise, <see langword="null"/>.</returns>
    Task<Guid?> WaitForSagaInStateAsync(Guid correlationId, IState state, TimeSpan? timeout = default,
        CancellationToken cancellationToken = default);

    /// <summary>Waits until at least one saga matching a predicate reaches a selected state.</summary>
    /// <param name="expression">The saga predicate.</param>
    /// <param name="stateSelector">The selector for the expected state.</param>
    /// <param name="timeout">The polling timeout, or <see langword="null"/> to use the harness default.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The matching correlation identifiers, or an empty list when the timeout expires.</returns>
    Task<IReadOnlyList<Guid>> WaitForSagasInStateAsync(Expression<Func<TInstance, bool>> expression,
        Func<TStateMachine, IState> stateSelector, TimeSpan? timeout = default,
        CancellationToken cancellationToken = default);

    /// <summary>Waits until at least one saga matching a predicate reaches a specified state.</summary>
    /// <param name="expression">The saga predicate.</param>
    /// <param name="state">The expected state.</param>
    /// <param name="timeout">The polling timeout, or <see langword="null"/> to use the harness default.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The matching correlation identifiers, or an empty list when the timeout expires.</returns>
    Task<IReadOnlyList<Guid>> WaitForSagasInStateAsync(Expression<Func<TInstance, bool>> expression, IState state,
        TimeSpan? timeout = default, CancellationToken cancellationToken = default);
}
