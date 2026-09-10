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
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Tests.Testing.SagaPollingTests.PollingSaga?>(cancellationToken); PollingSaga? saga = load(++LoadCount);
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
}
