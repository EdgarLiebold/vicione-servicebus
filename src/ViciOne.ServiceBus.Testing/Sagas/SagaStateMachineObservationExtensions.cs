using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Queries recorded and persisted state-machine saga state during tests.</summary>
public static class SagaStateMachineObservationExtensions
{
    static readonly TimeSpan _pollInterval = TimeSpan.FromMilliseconds(10);

    /// <summary>Finds a recorded saga in the state selected from a state machine.</summary>
    /// <typeparam name="TStateMachine">The state-machine implementation.</typeparam>
    /// <typeparam name="TInstance">The saga state type.</typeparam>
    /// <param name="sagas">The recorded saga observations.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="stateMachine">The state machine that defines the state.</param>
    /// <param name="stateSelector">The selector for the expected state.</param>
    /// <returns>The matching recorded saga, or <see langword="null"/>.</returns>
    public static TInstance? FindByIdInState<TStateMachine, TInstance>(this ISagaList<TInstance> sagas,
        Guid correlationId, TStateMachine stateMachine, Func<TStateMachine, State> stateSelector)
        where TStateMachine : SagaStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        ArgumentNullException.ThrowIfNull(sagas);
        ArgumentNullException.ThrowIfNull(stateMachine);
        ArgumentNullException.ThrowIfNull(stateSelector);
        return sagas.FindByIdInState(correlationId, stateMachine, stateSelector(stateMachine));
    }

    /// <summary>Finds a recorded saga in the specified state.</summary>
    /// <typeparam name="TInstance">The saga state type.</typeparam>
    /// <param name="sagas">The recorded saga observations.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="stateMachine">The state machine that defines the state.</param>
    /// <param name="state">The expected state.</param>
    /// <returns>The matching recorded saga, or <see langword="null"/>.</returns>
    public static TInstance? FindByIdInState<TInstance>(this ISagaList<TInstance> sagas, Guid correlationId,
        SagaStateMachine<TInstance> stateMachine, State state)
        where TInstance : class, SagaStateMachineInstance
    {
        ArgumentNullException.ThrowIfNull(sagas);
        ArgumentNullException.ThrowIfNull(stateMachine);
        ArgumentNullException.ThrowIfNull(state);
        Func<TInstance, bool> filter = stateMachine.CreateSagaFilter(
            instance => instance.CorrelationId == correlationId,
            state);

        return sagas.Snapshot(instance => filter(instance)).Select(observation => observation.Saga).LastOrDefault();
    }

    /// <summary>Waits until a saga reaches the state selected from a state machine.</summary>
    /// <typeparam name="TStateMachine">The state-machine implementation.</typeparam>
    /// <typeparam name="TInstance">The saga state type.</typeparam>
    /// <param name="repository">The saga repository.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="stateMachine">The state machine that defines the state.</param>
    /// <param name="stateSelector">The selector for the expected state.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier when the saga reaches the state; otherwise, <see langword="null"/>.</returns>
    public static Task<Guid?> WaitForSagaInStateAsync<TStateMachine, TInstance>(this ISagaRepository<TInstance> repository,
        Guid correlationId, TStateMachine stateMachine, Func<TStateMachine, State> stateSelector, TimeSpan timeout,
        CancellationToken cancellationToken = default)
        where TStateMachine : SagaStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        return repository.WaitForSagaInStateAsync(
            correlationId,
            stateMachine,
            stateSelector,
            timeout,
            TimeProvider.System,
            cancellationToken);
    }

    /// <summary>Waits until a saga reaches the state selected from a state machine.</summary>
    /// <typeparam name="TStateMachine">The state-machine implementation.</typeparam>
    /// <typeparam name="TInstance">The saga state type.</typeparam>
    /// <param name="repository">The saga repository.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="stateMachine">The state machine that defines the state.</param>
    /// <param name="stateSelector">The selector for the expected state.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="timeProvider">The time provider used by polling delays.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier when the saga reaches the state; otherwise, <see langword="null"/>.</returns>
    public static Task<Guid?> WaitForSagaInStateAsync<TStateMachine, TInstance>(this ISagaRepository<TInstance> repository,
        Guid correlationId, TStateMachine stateMachine, Func<TStateMachine, State> stateSelector, TimeSpan timeout,
        TimeProvider timeProvider, CancellationToken cancellationToken = default)
        where TStateMachine : SagaStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        ArgumentNullException.ThrowIfNull(stateMachine);
        ArgumentNullException.ThrowIfNull(stateSelector);
        return repository.WaitForSagaInStateAsync(
            correlationId,
            stateMachine,
            stateSelector(stateMachine),
            timeout,
            timeProvider,
            cancellationToken);
    }

    /// <summary>Waits until a saga reaches the specified state.</summary>
    /// <typeparam name="TStateMachine">The state-machine implementation.</typeparam>
    /// <typeparam name="TInstance">The saga state type.</typeparam>
    /// <param name="repository">The saga repository.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="stateMachine">The state machine that defines the state.</param>
    /// <param name="state">The expected state.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier when the saga reaches the state; otherwise, <see langword="null"/>.</returns>
    public static Task<Guid?> WaitForSagaInStateAsync<TStateMachine, TInstance>(this ISagaRepository<TInstance> repository,
        Guid correlationId, TStateMachine stateMachine, State state, TimeSpan timeout,
        CancellationToken cancellationToken = default)
        where TStateMachine : SagaStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        return repository.WaitForSagaInStateAsync(
            correlationId,
            stateMachine,
            state,
            timeout,
            TimeProvider.System,
            cancellationToken);
    }

    /// <summary>Waits until a saga reaches the specified state.</summary>
    /// <typeparam name="TStateMachine">The state-machine implementation.</typeparam>
    /// <typeparam name="TInstance">The saga state type.</typeparam>
    /// <param name="repository">The saga repository.</param>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="stateMachine">The state machine that defines the state.</param>
    /// <param name="state">The expected state.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="timeProvider">The time provider used by polling delays.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>The correlation identifier when the saga reaches the state; otherwise, <see langword="null"/>.</returns>
    public static Task<Guid?> WaitForSagaInStateAsync<TStateMachine, TInstance>(this ISagaRepository<TInstance> repository,
        Guid correlationId, TStateMachine stateMachine, State state, TimeSpan timeout, TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
        where TStateMachine : SagaStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        return repository.WaitForSagaInStateAsync(
            instance => instance.CorrelationId == correlationId,
            stateMachine,
            state,
            timeout,
            timeProvider,
            cancellationToken);
    }

    /// <summary>Waits until a saga matching a predicate reaches a selected state.</summary>
    /// <typeparam name="TStateMachine">The state-machine implementation.</typeparam>
    /// <typeparam name="TInstance">The saga state type.</typeparam>
    /// <param name="repository">The saga repository.</param>
    /// <param name="filter">The saga predicate.</param>
    /// <param name="stateMachine">The state machine that defines the state.</param>
    /// <param name="stateSelector">The selector for the expected state.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>A matching correlation identifier, or <see langword="null"/> when the timeout expires.</returns>
    public static Task<Guid?> WaitForSagaInStateAsync<TStateMachine, TInstance>(this ISagaRepository<TInstance> repository,
        Expression<Func<TInstance, bool>> filter, TStateMachine stateMachine,
        Func<TStateMachine, State> stateSelector, TimeSpan timeout, CancellationToken cancellationToken = default)
        where TStateMachine : SagaStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        return repository.WaitForSagaInStateAsync(
            filter,
            stateMachine,
            stateSelector,
            timeout,
            TimeProvider.System,
            cancellationToken);
    }

    /// <summary>Waits until a saga matching a predicate reaches a selected state.</summary>
    /// <typeparam name="TStateMachine">The state-machine implementation.</typeparam>
    /// <typeparam name="TInstance">The saga state type.</typeparam>
    /// <param name="repository">The saga repository.</param>
    /// <param name="filter">The saga predicate.</param>
    /// <param name="stateMachine">The state machine that defines the state.</param>
    /// <param name="stateSelector">The selector for the expected state.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="timeProvider">The time provider used by polling delays.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>A matching correlation identifier, or <see langword="null"/> when the timeout expires.</returns>
    public static Task<Guid?> WaitForSagaInStateAsync<TStateMachine, TInstance>(this ISagaRepository<TInstance> repository,
        Expression<Func<TInstance, bool>> filter, TStateMachine stateMachine,
        Func<TStateMachine, State> stateSelector, TimeSpan timeout, TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
        where TStateMachine : SagaStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        ArgumentNullException.ThrowIfNull(stateMachine);
        ArgumentNullException.ThrowIfNull(stateSelector);
        return repository.WaitForSagaInStateAsync(
            filter,
            stateMachine,
            stateSelector(stateMachine),
            timeout,
            timeProvider,
            cancellationToken);
    }

    /// <summary>Waits until a saga matching a predicate reaches the specified state.</summary>
    /// <typeparam name="TStateMachine">The state-machine implementation.</typeparam>
    /// <typeparam name="TInstance">The saga state type.</typeparam>
    /// <param name="repository">The saga repository.</param>
    /// <param name="filter">The saga predicate.</param>
    /// <param name="stateMachine">The state machine that defines the state.</param>
    /// <param name="state">The expected state.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>A matching correlation identifier, or <see langword="null"/> when the timeout expires.</returns>
    public static Task<Guid?> WaitForSagaInStateAsync<TStateMachine, TInstance>(this ISagaRepository<TInstance> repository,
        Expression<Func<TInstance, bool>> filter, TStateMachine stateMachine, State state, TimeSpan timeout,
        CancellationToken cancellationToken = default)
        where TStateMachine : SagaStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        return repository.WaitForSagaInStateAsync(
            filter,
            stateMachine,
            state,
            timeout,
            TimeProvider.System,
            cancellationToken);
    }

    /// <summary>Waits until a saga matching a predicate reaches the specified state.</summary>
    /// <typeparam name="TStateMachine">The state-machine implementation.</typeparam>
    /// <typeparam name="TInstance">The saga state type.</typeparam>
    /// <param name="repository">The saga repository.</param>
    /// <param name="filter">The saga predicate.</param>
    /// <param name="stateMachine">The state machine that defines the state.</param>
    /// <param name="state">The expected state.</param>
    /// <param name="timeout">The maximum polling duration.</param>
    /// <param name="timeProvider">The time provider used by polling delays.</param>
    /// <param name="cancellationToken">The token that cancels repository reads and polling delays.</param>
    /// <returns>A matching correlation identifier, or <see langword="null"/> when the timeout expires.</returns>
    public static async Task<Guid?> WaitForSagaInStateAsync<TStateMachine, TInstance>(
        this ISagaRepository<TInstance> repository, Expression<Func<TInstance, bool>> filter,
        TStateMachine stateMachine, State state, TimeSpan timeout, TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
        where TStateMachine : SagaStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(stateMachine);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (timeout < TimeSpan.Zero || timeout == Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The timeout must be non-negative.");
        if (repository is not IQuerySagaRepository<TInstance> queryRepository)
            throw new ArgumentException("The repository must support querying sagas.", nameof(repository));

        cancellationToken.ThrowIfCancellationRequested();
        ISagaQuery<TInstance> query = stateMachine.CreateSagaQuery(filter, state);
        var startedAt = timeProvider.GetTimestamp();
        while (true)
        {
            Guid sagaId = (await queryRepository.FindAsync(query, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
            if (sagaId != Guid.Empty)
                return sagaId;

            TimeSpan remaining = timeout - timeProvider.GetElapsedTime(startedAt);
            if (remaining <= TimeSpan.Zero)
                return null;

            await Task.Delay(remaining < _pollInterval ? remaining : _pollInterval, timeProvider, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
