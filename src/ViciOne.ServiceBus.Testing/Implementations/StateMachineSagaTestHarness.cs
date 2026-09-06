using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Provides a test harness for state machine saga test.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TStateMachine">The state machine type.</typeparam>
public class StateMachineSagaTestHarness<TInstance, TStateMachine> :
    SagaTestHarness<TInstance>,
    ISagaStateMachineTestHarness<TStateMachine, TInstance>
    where TInstance : class, SagaStateMachineInstance
    where TStateMachine : SagaStateMachine<TInstance>
{
    readonly StateMachineObservationCollector<TInstance> _observations;
    readonly IDisposable _eventObserverHandle;
    readonly IDisposable _stateObserverHandle;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="testHarness">The test harness.</param>
    /// <param name="repository">The repository.</param>
    /// <param name="querySagaRepository">The query saga repository.</param>
    /// <param name="loadSagaRepository">The load saga repository.</param>
    /// <param name="stateMachine">The state machine.</param>
    /// <param name="queueName">The queue name.</param>
    public StateMachineSagaTestHarness(BusTestHarness testHarness, ISagaRepository<TInstance> repository,
        IQuerySagaRepository<TInstance>? querySagaRepository, ILoadSagaRepository<TInstance>? loadSagaRepository, TStateMachine stateMachine,
        string? queueName)
        : base(testHarness, repository, querySagaRepository, loadSagaRepository, queueName)
    {
        StateMachine = stateMachine;
        _observations = new StateMachineObservationCollector<TInstance>(testHarness.ContextSaveMode, testHarness.MaximumSavedContexts);
        _eventObserverHandle = StateMachine.ConnectEventObserver(_observations);
        _stateObserverHandle = StateMachine.ConnectStateObserver(_observations);
    }

    /// <summary>Gets the state machine.</summary>
    public TStateMachine StateMachine { get; }

    /// <summary>Gets the events.</summary>
    public IReadOnlyList<StateMachineEventObservation> Events => _observations.Events;
    /// <summary>Gets the state changes.</summary>
    public IReadOnlyList<StateMachineStateChange> StateChanges => _observations.StateChanges;

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        _stateObserverHandle.Dispose();
        _eventObserverHandle.Dispose();
    }

    /// <summary>Waits until a saga exists with the specified correlationId in the specified state.</summary>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="stateSelector">The state selector.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the exists outcome.</returns>
    public Task<Guid?> ExistsAsync(Guid correlationId, Func<TStateMachine, State> stateSelector, TimeSpan? timeout = default, CancellationToken cancellationToken = default)
    {
        var state = stateSelector(StateMachine);

        return ExistsAsync(correlationId, state, timeout, cancellationToken: cancellationToken);
    }

    /// <summary>Waits until a saga exists with the specified correlationId in the specified state.</summary>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="state">The expected state.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the exists outcome.</returns>
    public async Task<Guid?> ExistsAsync(Guid correlationId, State state, TimeSpan? timeout = default, CancellationToken cancellationToken = default)
    {
        if (QuerySagaRepository == null)
            throw new InvalidOperationException("The repository does not support Query operations");

        ISagaQuery<TInstance> query = StateMachine.CreateSagaQuery(x => x.CorrelationId == correlationId, state);

        return await PollAsync(
            async () => (Guid?)(await QuerySagaRepository.FindAsync(query, cancellationToken: cancellationToken).ConfigureAwait(false)).FirstOrDefault(),
            sagaId => sagaId.HasValue && sagaId.Value != Guid.Empty,
            default(Guid?),
            timeout).ConfigureAwait(false);
    }

    /// <summary>Waits until a saga exists with the specified correlationId in the specified state.</summary>
    /// <param name="expression">The expression.</param>
    /// <param name="stateSelector">The state selector.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the exists outcome.</returns>
    public Task<IList<Guid>> ExistsAsync(Expression<Func<TInstance, bool>> expression, Func<TStateMachine, State> stateSelector, TimeSpan? timeout = default, CancellationToken cancellationToken = default)
    {
        var state = stateSelector(StateMachine);

        return ExistsAsync(expression, state, timeout, cancellationToken: cancellationToken);
    }

    /// <summary>Waits until a saga exists with the specified correlationId in the specified state.</summary>
    /// <param name="expression">The expression.</param>
    /// <param name="state">The expected state.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the exists outcome.</returns>
    public async Task<IList<Guid>> ExistsAsync(Expression<Func<TInstance, bool>> expression, State state, TimeSpan? timeout = default, CancellationToken cancellationToken = default)
    {
        if (QuerySagaRepository == null)
            throw new InvalidOperationException("The repository does not support Query operations");

        ISagaQuery<TInstance> query = StateMachine.CreateSagaQuery(expression, state);

        return await PollAsync(
            async () => (IList<Guid>)(await QuerySagaRepository.FindAsync(query, cancellationToken: cancellationToken).ConfigureAwait(false)).ToList(),
            sagas => sagas.Count > 0,
            new List<Guid>(),
            timeout).ConfigureAwait(false);
    }

    /// <summary>Configures receive endpoint.</summary>
    /// <param name="configurator">The configurator to update.</param>
    protected override void ConfigureReceiveEndpoint(IReceiveEndpointConfigurator configurator)
    {
        configurator.StateMachineSaga(StateMachine, TestRepository);
    }

    /// <summary>Configures named receive endpoint.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="queueName">The queue name.</param>
    protected override void ConfigureNamedReceiveEndpoint(IBusFactoryConfigurator configurator, string queueName)
    {
        configurator.ReceiveEndpoint(queueName, x =>
        {
            x.StateMachineSaga(StateMachine, TestRepository);
        });
    }
}
