using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Internal;

/// <summary>Records repository and execution activity for a state-machine saga.</summary>
/// <typeparam name="TStateMachine">The state-machine implementation.</typeparam>
/// <typeparam name="TInstance">The saga state type.</typeparam>
internal sealed class StateMachineSagaTestHarness<TStateMachine, TInstance> :
    SagaTestHarness<TInstance>,
    ISagaStateMachineTestHarness<TStateMachine, TInstance>
    where TStateMachine : ISagaStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    readonly IDisposable _eventObserverHandle;
    readonly StateMachineObservationCollector<TInstance> _observations;
    readonly IDisposable _stateObserverHandle;
    int _disposed;

    /// <summary>Registers an observed state machine and its saga repository with a bus test harness.</summary>
    /// <param name="testHarness">The bus harness that hosts the saga endpoint.</param>
    /// <param name="repository">The saga repository to decorate.</param>
    /// <param name="querySagaRepository">The optional repository query capability.</param>
    /// <param name="loadSagaRepository">The optional repository load capability.</param>
    /// <param name="stateMachine">The state machine to observe.</param>
    /// <param name="queueName">The dedicated endpoint queue, or <see langword="null"/> for the harness endpoint.</param>
    public StateMachineSagaTestHarness(BusTestHarness testHarness, ISagaRepository<TInstance> repository,
        IQuerySagaRepository<TInstance>? querySagaRepository, ILoadSagaRepository<TInstance>? loadSagaRepository,
        TStateMachine stateMachine, string? queueName)
        : base(testHarness, repository, querySagaRepository, loadSagaRepository, queueName)
    {
        StateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _observations = new StateMachineObservationCollector<TInstance>(
            testHarness.ContextSaveMode,
            testHarness.MaximumSavedContexts);
        _eventObserverHandle = StateMachine.ConnectEventObserver(_observations);
        _stateObserverHandle = StateMachine.ConnectStateObserver(_observations);
    }

    /// <inheritdoc />
    public TStateMachine StateMachine { get; }

    /// <inheritdoc />
    public IReadOnlyList<StateMachineEventObservation> Events => _observations.Events;

    /// <inheritdoc />
    public IReadOnlyList<StateMachineStateChange> StateChanges => _observations.StateChanges;

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _stateObserverHandle.Dispose();
        _eventObserverHandle.Dispose();
    }

    /// <inheritdoc />
    public Task<Guid?> WaitForSagaInStateAsync(Guid correlationId, Func<TStateMachine, IState> stateSelector,
        TimeSpan? timeout = default, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stateSelector);
        return WaitForSagaInStateAsync(correlationId, stateSelector(StateMachine), timeout, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Guid?> WaitForSagaInStateAsync(Guid correlationId, IState state, TimeSpan? timeout = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (QuerySagaRepository == null)
            throw new InvalidOperationException($"The {typeof(TInstance).Name} repository does not support querying sagas.");

        ISagaQuery<TInstance> query = StateMachine.CreateSagaQuery(
            instance => instance.CorrelationId == correlationId,
            state);

        return await PollAsync(
            async () => (Guid?)(await QuerySagaRepository.FindAsync(query, cancellationToken).ConfigureAwait(false)).FirstOrDefault(),
            sagaId => sagaId.HasValue && sagaId.Value != Guid.Empty,
            default(Guid?),
            timeout,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Guid>> WaitForSagasInStateAsync(Expression<Func<TInstance, bool>> expression,
        Func<TStateMachine, IState> stateSelector, TimeSpan? timeout = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stateSelector);
        return WaitForSagasInStateAsync(expression, stateSelector(StateMachine), timeout, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> WaitForSagasInStateAsync(Expression<Func<TInstance, bool>> expression, IState state,
        TimeSpan? timeout = default, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(state);
        if (QuerySagaRepository == null)
            throw new InvalidOperationException($"The {typeof(TInstance).Name} repository does not support querying sagas.");

        ISagaQuery<TInstance> query = StateMachine.CreateSagaQuery(expression, state);

        return await PollAsync(
            async () => (IReadOnlyList<Guid>)(await QuerySagaRepository.FindAsync(query, cancellationToken)
                .ConfigureAwait(false)).ToList(),
            sagaIds => sagaIds.Count > 0,
            Array.Empty<Guid>(),
            timeout,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override void ConfigureReceiveEndpoint(IReceiveEndpointConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.StateMachineSaga(StateMachine, TestRepository);
    }

    /// <inheritdoc />
    protected override void ConfigureNamedReceiveEndpoint(IBusFactoryConfigurator configurator, string queueName)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        configurator.ReceiveEndpoint(queueName, endpoint => endpoint.StateMachineSaga(StateMachine, TestRepository));
    }
}
