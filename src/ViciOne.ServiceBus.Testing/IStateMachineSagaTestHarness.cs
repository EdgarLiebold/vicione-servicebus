using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

public interface ISagaStateMachineTestHarness<out TStateMachine, TInstance> :
    ISagaTestHarness<TInstance>,
    IDisposable
    where TStateMachine : SagaStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    TStateMachine StateMachine { get; }

    IReadOnlyList<StateMachineEventObservation> Events { get; }

    IReadOnlyList<StateMachineStateChange> StateChanges { get; }

    /// <summary>
    /// Waits until a saga exists with the specified correlationId in the specified state
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="stateSelector"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    Task<Guid?> Exists(Guid correlationId, Func<TStateMachine, State> stateSelector, TimeSpan? timeout = default);

    /// <summary>
    /// Waits until a saga exists with the specified correlationId in the specified state
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="state">The expected state</param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    Task<Guid?> Exists(Guid correlationId, State state, TimeSpan? timeout = default);

    /// <summary>
    /// Waits until a saga exists with the specified correlationId in the specified state
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="stateSelector"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    Task<IList<Guid>> Exists(Expression<Func<TInstance, bool>> expression, Func<TStateMachine, State> stateSelector, TimeSpan? timeout = default);

    /// <summary>
    /// Waits until a saga exists with the specified correlationId in the specified state
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="state">The expected state</param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    Task<IList<Guid>> Exists(Expression<Func<TInstance, bool>> expression, State state, TimeSpan? timeout = default);
}
