using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Defines the operations required by saga state machine test harness.</summary>
/// <typeparam name="TStateMachine">The state machine type.</typeparam>
/// <typeparam name="TInstance">The instance type.</typeparam>
public interface ISagaStateMachineTestHarness<out TStateMachine, TInstance> :
    ISagaTestHarness<TInstance>,
    IDisposable
    where TStateMachine : SagaStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>Gets the state machine.</summary>
    TStateMachine StateMachine { get; }

    /// <summary>Gets the events.</summary>
    IReadOnlyList<StateMachineEventObservation> Events { get; }

    /// <summary>Gets the state changes.</summary>
    IReadOnlyList<StateMachineStateChange> StateChanges { get; }

    /// <summary>Waits until a saga exists with the specified correlationId in the specified state.</summary>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="stateSelector">The state selector.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the exists outcome.</returns>
    Task<Guid?> ExistsAsync(Guid correlationId, Func<TStateMachine, State> stateSelector, TimeSpan? timeout = default, CancellationToken cancellationToken = default);

    /// <summary>Waits until a saga exists with the specified correlationId in the specified state.</summary>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="state">The expected state.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the exists outcome.</returns>
    Task<Guid?> ExistsAsync(Guid correlationId, State state, TimeSpan? timeout = default, CancellationToken cancellationToken = default);

    /// <summary>Waits until a saga exists with the specified correlationId in the specified state.</summary>
    /// <param name="expression">The expression.</param>
    /// <param name="stateSelector">The state selector.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the exists outcome.</returns>
    Task<IList<Guid>> ExistsAsync(Expression<Func<TInstance, bool>> expression, Func<TStateMachine, State> stateSelector, TimeSpan? timeout = default, CancellationToken cancellationToken = default);

    /// <summary>Waits until a saga exists with the specified correlationId in the specified state.</summary>
    /// <param name="expression">The expression.</param>
    /// <param name="state">The expected state.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the exists outcome.</returns>
    Task<IList<Guid>> ExistsAsync(Expression<Func<TInstance, bool>> expression, State state, TimeSpan? timeout = default, CancellationToken cancellationToken = default);
}
