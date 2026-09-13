using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class SagaPollingTests
{
    private static readonly DateTimeOffset StartTime =
        new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-SAGA-POLLING", "virtual-time-timeout")]
    public async Task MissingSaga_TimesOutOnTheConfiguredClockWithoutWallClockDelayAsync()
    {
        TimeSpan timeout = TimeSpan.FromMinutes(1);
        var timeProvider = new ObservableTimeProvider(StartTime);
        var repository = new LoadSagaRepository(_ => null);

        Task<Guid?> observation = repository.WaitForSagaAsync(NewId.NextGuid(), timeout, timeProvider, cancellationToken: TestContext.Current.CancellationToken);
        await timeProvider.WaitForTimerCountAsync(1).WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        timeProvider.Advance(timeout);

        Assert.Null(await observation.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken));
        Assert.Equal(2, repository.LoadCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-SAGA-POLLING", "eventual-match")]
    public async Task SagaAppearingAfterAPoll_IsReturnedOnTheNextVirtualIntervalAsync()
    {
        Guid sagaId = NewId.NextGuid();
        var timeProvider = new ObservableTimeProvider(StartTime);
        var repository = new LoadSagaRepository(call => call == 1 ? null : new PollingSaga(sagaId));

        Task<Guid?> observation = repository.WaitForSagaAsync(sagaId, TimeSpan.FromMinutes(1), timeProvider, cancellationToken: TestContext.Current.CancellationToken);
        await timeProvider.WaitForTimerCountAsync(1).WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeSpan.FromMilliseconds(10));

        Assert.Equal(sagaId, await observation.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken));
        Assert.Equal(2, repository.LoadCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-SAGA-POLLING", "zero-timeout-immediate-probe")]
    public async Task ZeroTimeout_PerformsOneImmediateRepositoryProbeAsync()
    {
        Guid sagaId = NewId.NextGuid();
        var repository = new LoadSagaRepository(_ => new PollingSaga(sagaId));

        Guid? observed = await repository.WaitForSagaAsync(
            sagaId,
            TimeSpan.Zero,
            TimeProvider.System,
            TestContext.Current.CancellationToken);

        Assert.Equal(sagaId, observed);
        Assert.Equal(1, repository.LoadCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-SAGA-POLLING", "eventual-removal")]
    public async Task SagaRemoval_ReturnsTheCorrelationIdAfterAbsenceIsObservedAsync()
    {
        Guid sagaId = NewId.NextGuid();
        var timeProvider = new ObservableTimeProvider(StartTime);
        var repository = new LoadSagaRepository(call => call == 1 ? new PollingSaga(sagaId) : null);

        Task<Guid?> observation = repository.WaitForSagaRemovalAsync(
            sagaId,
            TimeSpan.FromMinutes(1),
            timeProvider,
            TestContext.Current.CancellationToken);
        await timeProvider.WaitForTimerCountAsync(1).WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeSpan.FromMilliseconds(10));

        Assert.Equal(sagaId, await observation.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken));
        Assert.Equal(2, repository.LoadCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-SAGA-POLLING", "removal-timeout-distinguishable")]
    public async Task SagaRemoval_ReturnsNullWhenTheSagaRemainsUntilTheDeadlineAsync()
    {
        Guid sagaId = NewId.NextGuid();
        TimeSpan timeout = TimeSpan.FromMinutes(1);
        var timeProvider = new ObservableTimeProvider(StartTime);
        var repository = new LoadSagaRepository(_ => new PollingSaga(sagaId));

        Task<Guid?> observation = repository.WaitForSagaRemovalAsync(
            sagaId,
            timeout,
            timeProvider,
            TestContext.Current.CancellationToken);
        await timeProvider.WaitForTimerCountAsync(1).WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        timeProvider.Advance(timeout);

        Assert.Null(await observation.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken));
        Assert.Equal(2, repository.LoadCount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-2)]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-SAGA-POLLING", "invalid-timeout-rejected")]
    public async Task RepositoryPolling_RejectsNegativeAndInfiniteTimeoutsAsync(long timeoutTicks)
    {
        TimeSpan timeout = timeoutTicks == -1 ? Timeout.InfiniteTimeSpan : TimeSpan.FromTicks(timeoutTicks);
        var repository = new LoadSagaRepository(_ => null);

        ArgumentOutOfRangeException exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            repository.WaitForSagaAsync(
                NewId.NextGuid(),
                timeout,
                TimeProvider.System,
                TestContext.Current.CancellationToken));

        Assert.Equal("timeout", exception.ParamName);
        Assert.Equal(0, repository.LoadCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-SAGA-POLLING", "unsupported-repository-capability")]
    public async Task RepositoryWithoutLoadOrQueryCapability_IsRejectedPreciselyAsync()
    {
        var repository = new DispatchOnlySagaRepository();

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.WaitForSagaAsync(NewId.NextGuid(), TimeSpan.FromSeconds(1), cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("repository", exception.ParamName);
        Assert.Contains("loading or querying sagas", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CANCELLATION", "state-poll-delay-observes-caller-token")]
    public async Task StatePolling_CallerCancellationDisposesTheActiveDelayAndPreservesTheExactTokenAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var repository = new QuerySagaRepository();
        var machine = new PollingStateMachine();
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        Task<Guid?> observation = repository.WaitForSagaInStateAsync(
            NewId.NextGuid(),
            machine,
            machine.Initial,
            TimeSpan.FromMinutes(1),
            timeProvider,
            source.Token);
        await timeProvider.WaitForTimerCountAsync(1).WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        Assert.Equal(1, timeProvider.ActiveTimerCount);

        source.Cancel();

        Assert.Equal(0, timeProvider.ActiveTimerCount);
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => observation);
        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.Equal(1, repository.QueryCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CANCELLATION", "repository-poll-delay-observes-caller-token")]
    public async Task RepositoryPolling_CallerCancellationDisposesTheActiveDelayAndPreservesTheExactTokenAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var repository = new LoadSagaRepository(_ => null);
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        Task<Guid?> observation = repository.WaitForSagaAsync(
            NewId.NextGuid(),
            TimeSpan.FromMinutes(1),
            timeProvider,
            source.Token);
        await timeProvider.WaitForTimerCountAsync(1).WaitAsync(
            OperationTimeout(),
            TestContext.Current.CancellationToken);
        Assert.Equal(1, timeProvider.ActiveTimerCount);

        source.Cancel();

        Assert.Equal(0, timeProvider.ActiveTimerCount);
        OperationCanceledException exception =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => observation);
        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.Equal(1, repository.LoadCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-SAGA-POLLING", "all-repository-capability-overloads")]
    public async Task RepositoryOverloads_RouteEveryDeclaredCapabilityAndPreserveTheMatchedIdentityAsync()
    {
        Guid sagaId = NewId.NextGuid();
        var saga = new PollingSaga(sagaId);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        ILoadSagaRepository<PollingSaga> loadRepository = new LoadSagaRepository(_ => saga);
        ILoadSagaRepository<PollingSaga> emptyLoadRepository = new LoadSagaRepository(_ => null);
        IQuerySagaRepository<PollingSaga> queryRepository = new QueryAndDispatchSagaRepository<PollingSaga>([saga]);
        IQuerySagaRepository<PollingSaga> emptyQueryRepository = new QueryAndDispatchSagaRepository<PollingSaga>([]);
        ISagaRepository<PollingSaga> dispatchLoadRepository = new LoadAndDispatchSagaRepository<PollingSaga>(_ => saga);
        ISagaRepository<PollingSaga> emptyDispatchLoadRepository = new LoadAndDispatchSagaRepository<PollingSaga>(_ => null);
        ISagaRepository<PollingSaga> dispatchQueryRepository = new QueryAndDispatchSagaRepository<PollingSaga>([saga]);
        ISagaRepository<PollingSaga> emptyDispatchQueryRepository = new QueryAndDispatchSagaRepository<PollingSaga>([]);

        Assert.Equal(sagaId, await loadRepository.WaitForSagaAsync(sagaId, TimeSpan.Zero, cancellationToken));
        Assert.Equal(sagaId, await loadRepository.WaitForSagaAsync(
            sagaId,
            candidate => candidate.CorrelationId == sagaId,
            TimeSpan.Zero,
            cancellationToken));
        Assert.Equal(sagaId, await emptyLoadRepository.WaitForSagaRemovalAsync(sagaId, TimeSpan.Zero, cancellationToken));

        Assert.Equal(sagaId, await queryRepository.WaitForSagaAsync(sagaId, TimeSpan.Zero, cancellationToken));
        Assert.Equal(sagaId, await queryRepository.WaitForSagaAsync(
            candidate => candidate.CorrelationId == sagaId,
            TimeSpan.Zero,
            cancellationToken));
        Assert.Equal(sagaId, await emptyQueryRepository.WaitForSagaRemovalAsync(sagaId, TimeSpan.Zero, cancellationToken));

        Assert.Equal(sagaId, await dispatchLoadRepository.WaitForSagaAsync(sagaId, TimeSpan.Zero, cancellationToken));
        Assert.Equal(sagaId, await dispatchLoadRepository.WaitForSagaAsync(
            sagaId,
            candidate => candidate.CorrelationId == sagaId,
            TimeSpan.Zero,
            cancellationToken));
        Assert.Equal(sagaId, await emptyDispatchLoadRepository.WaitForSagaRemovalAsync(sagaId, TimeSpan.Zero, cancellationToken));

        Assert.Equal(sagaId, await dispatchQueryRepository.WaitForSagaAsync(sagaId, TimeSpan.Zero, cancellationToken));
        Assert.Equal(sagaId, await dispatchQueryRepository.WaitForSagaAsync(
            candidate => candidate.CorrelationId == sagaId,
            TimeSpan.Zero,
            cancellationToken));
        Assert.Equal(sagaId, await emptyDispatchQueryRepository.WaitForSagaRemovalAsync(sagaId, TimeSpan.Zero, cancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-STATE-MACHINE-OBSERVATION", "all-state-selector-and-filter-overloads")]
    public async Task StateMachineObservationOverloads_PreserveRecordedAndPersistedStateMatchesAsync()
    {
        Guid sagaId = NewId.NextGuid();
        var machine = new PollingStateMachine();
        var instance = new PollingState
        {
            CorrelationId = sagaId,
            CurrentState = machine.Initial.Name,
        };
        ISagaList<PollingState> observations = new StaticSagaList<PollingState>([instance]);
        ISagaRepository<PollingState> repository = new QueryAndDispatchSagaRepository<PollingState>([instance]);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        Assert.Same(instance, observations.FindByIdInState(sagaId, machine, selected => selected.Initial));
        Assert.Same(instance, observations.FindByIdInState(sagaId, machine, machine.Initial));
        Assert.Equal(sagaId, await repository.WaitForSagaInStateAsync(
            sagaId,
            machine,
            selected => selected.Initial,
            TimeSpan.Zero,
            cancellationToken));
        Assert.Equal(sagaId, await repository.WaitForSagaInStateAsync(
            sagaId,
            machine,
            machine.Initial,
            TimeSpan.Zero,
            cancellationToken));
        Assert.Equal(sagaId, await repository.WaitForSagaInStateAsync(
            candidate => candidate.CorrelationId == sagaId,
            machine,
            selected => selected.Initial,
            TimeSpan.Zero,
            cancellationToken));
        Assert.Equal(sagaId, await repository.WaitForSagaInStateAsync(
            candidate => candidate.CorrelationId == sagaId,
            machine,
            machine.Initial,
            TimeSpan.Zero,
            cancellationToken));
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed class PollingSaga(Guid correlationId) : ISaga
    {
        public Guid CorrelationId { get; set; } = correlationId;
    }

    private sealed class LoadSagaRepository(Func<int, PollingSaga?> load) : ILoadSagaRepository<PollingSaga>
    {
        public int LoadCount { get; private set; }

        public Task<PollingSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<PollingSaga?>(cancellationToken);

            PollingSaga? saga = load(++LoadCount);
            return Task.FromResult(saga);
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class DispatchOnlySagaRepository : ISagaRepository<PollingSaga>
    {
        public Task SendAsync<T>(
            ConsumeContext<T> context,
            ISagaPolicy<PollingSaga, T> policy,
            IPipe<SagaConsumeContext<PollingSaga, T>> next)
            where T : class => Task.CompletedTask;

        public Task SendQueryAsync<T>(
            ConsumeContext<T> context,
            ISagaQuery<PollingSaga> query,
            ISagaPolicy<PollingSaga, T> policy,
            IPipe<SagaConsumeContext<PollingSaga, T>> next)
            where T : class => Task.CompletedTask;

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class PollingState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;
    }

    private sealed class PollingStateMachine : ViciOneServiceBusStateMachine<PollingState>
    {
        public PollingStateMachine()
        {
            InstanceState(instance => instance.CurrentState);
        }
    }

    private sealed class QuerySagaRepository : ISagaRepository<PollingState>, IQuerySagaRepository<PollingState>
    {
        public int QueryCount { get; private set; }

        public Task<IEnumerable<Guid>> FindAsync(ISagaQuery<PollingState> query, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            QueryCount++;
            return Task.FromResult<IEnumerable<Guid>>([]);
        }

        public Task SendAsync<T>(
            ConsumeContext<T> context,
            ISagaPolicy<PollingState, T> policy,
            IPipe<SagaConsumeContext<PollingState, T>> next)
            where T : class => Task.CompletedTask;

        public Task SendQueryAsync<T>(
            ConsumeContext<T> context,
            ISagaQuery<PollingState> query,
            ISagaPolicy<PollingState, T> policy,
            IPipe<SagaConsumeContext<PollingState, T>> next)
            where T : class => Task.CompletedTask;

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class LoadAndDispatchSagaRepository<TSaga>(Func<Guid, TSaga?> load) :
        ISagaRepository<TSaga>,
        ILoadSagaRepository<TSaga>
        where TSaga : class, ISaga
    {
        public Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(load(correlationId));
        }

        public Task SendAsync<T>(
            ConsumeContext<T> context,
            ISagaPolicy<TSaga, T> policy,
            IPipe<SagaConsumeContext<TSaga, T>> next)
            where T : class => Task.CompletedTask;

        public Task SendQueryAsync<T>(
            ConsumeContext<T> context,
            ISagaQuery<TSaga> query,
            ISagaPolicy<TSaga, T> policy,
            IPipe<SagaConsumeContext<TSaga, T>> next)
            where T : class => Task.CompletedTask;

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class QueryAndDispatchSagaRepository<TSaga>(IEnumerable<TSaga> sagas) :
        ISagaRepository<TSaga>,
        IQuerySagaRepository<TSaga>
        where TSaga : class, ISaga
    {
        private readonly TSaga[] _sagas = sagas.ToArray();

        public Task<IEnumerable<Guid>> FindAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Func<TSaga, bool> filter = query.FilterExpression.Compile();
            return Task.FromResult<IEnumerable<Guid>>(_sagas.Where(filter).Select(saga => saga.CorrelationId).ToArray());
        }

        public Task SendAsync<T>(
            ConsumeContext<T> context,
            ISagaPolicy<TSaga, T> policy,
            IPipe<SagaConsumeContext<TSaga, T>> next)
            where T : class => Task.CompletedTask;

        public Task SendQueryAsync<T>(
            ConsumeContext<T> context,
            ISagaQuery<TSaga> query,
            ISagaPolicy<TSaga, T> policy,
            IPipe<SagaConsumeContext<TSaga, T>> next)
            where T : class => Task.CompletedTask;

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class StaticSagaList<TSaga>(IEnumerable<TSaga> sagas) : ISagaList<TSaga>
        where TSaga : class, ISaga
    {
        private readonly ISagaInstance<TSaga>[] _observations = sagas.Select(saga => new StaticSagaInstance<TSaga>(saga)).ToArray();

        public int Count => _observations.Length;

        public TestContextSaveMode SaveMode => TestContextSaveMode.All;

        public int MaximumSavedElements => int.MaxValue;

        public IReadOnlyList<ISagaInstance<TSaga>> Snapshot() => _observations;

        public IReadOnlyList<ISagaInstance<TSaga>> Snapshot(FilterDelegate<TSaga> filter) =>
            _observations.Where(observation => filter(observation.Saga)).ToArray();

        public TSaga? FindById(Guid sagaId) =>
            _observations.Select(observation => observation.Saga).LastOrDefault(saga => saga.CorrelationId == sagaId);

        public IAsyncEnumerable<ISagaInstance<TSaga>> SelectAsync(FilterDelegate<ISagaInstance<TSaga>> filter,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IAsyncEnumerable<ISagaInstance<TSaga>> SelectAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<ISagaInstance<TSaga>> SelectAsync(FilterDelegate<TSaga> filter,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> AnyAsync(FilterDelegate<ISagaInstance<TSaga>> filter, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> AnyAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> AnyAsync(FilterDelegate<TSaga> filter, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StaticSagaInstance<TSaga>(TSaga saga) : ISagaInstance<TSaga>
        where TSaga : class, ISaga
    {
        public TSaga Saga { get; } = saga;

        public Guid? ElementId => Saga.CorrelationId;
    }
}
