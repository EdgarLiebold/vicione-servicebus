using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Defines the contract for saga state machine test harness.
/// </summary>
/// <typeparam name="TStateMachine">The t state machine type.</typeparam>
/// <typeparam name="TInstance">The t instance type.</typeparam>
public interface ISagaStateMachineTestHarness<out TStateMachine, TInstance> :
    ISagaTestHarness<TInstance>,
    IDisposable
    where TStateMachine : SagaStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>
    /// Gets the state machine value.
    /// </summary>
    TStateMachine StateMachine { get; }

    /// <summary>
    /// Gets the events value.
    /// </summary>
    IReadOnlyList<StateMachineEventObservation> Events { get; }

    /// <summary>
    /// Gets the state changes value.
    /// </summary>
    IReadOnlyList<StateMachineStateChange> StateChanges { get; }

    /// <summary>
    /// Waits until a saga exists with the specified correlationId in the specified state
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="stateSelector"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<Guid?> ExistsAsync(Guid correlationId, Func<TStateMachine, State> stateSelector, TimeSpan? timeout = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits until a saga exists with the specified correlationId in the specified state
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="state">The expected state</param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<Guid?> ExistsAsync(Guid correlationId, State state, TimeSpan? timeout = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits until a saga exists with the specified correlationId in the specified state
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="stateSelector"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<IList<Guid>> ExistsAsync(Expression<Func<TInstance, bool>> expression, Func<TStateMachine, State> stateSelector, TimeSpan? timeout = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits until a saga exists with the specified correlationId in the specified state
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="state">The expected state</param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<IList<Guid>> ExistsAsync(Expression<Func<TInstance, bool>> expression, State state, TimeSpan? timeout = default, CancellationToken cancellationToken = default);
}
