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

        Task<Guid?> observation = repository.ShouldContainSagaAsync(NewId.NextGuid(), timeout, timeProvider, cancellationToken: TestContext.Current.CancellationToken);
        await timeProvider.WaitForTimerCountAsync(1).WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        timeProvider.Advance(timeout);

        Assert.Null(await observation.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken));
        Assert.Equal(1, repository.LoadCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-SAGA-POLLING", "eventual-match")]
    public async Task SagaAppearingAfterAPoll_IsReturnedOnTheNextVirtualIntervalAsync()
    {
        Guid sagaId = NewId.NextGuid();
        var timeProvider = new ObservableTimeProvider(StartTime);
        var repository = new LoadSagaRepository(call => call == 1 ? null : new PollingSaga(sagaId));

        Task<Guid?> observation = repository.ShouldContainSagaAsync(sagaId, TimeSpan.FromMinutes(1), timeProvider, cancellationToken: TestContext.Current.CancellationToken);
        await timeProvider.WaitForTimerCountAsync(1).WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeSpan.FromMilliseconds(10));

        Assert.Equal(sagaId, await observation.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken));
        Assert.Equal(2, repository.LoadCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-SAGA-POLLING", "unsupported-repository-capability")]
    public async Task RepositoryWithoutLoadOrQueryCapability_IsRejectedPreciselyAsync()
    {
        var repository = new DispatchOnlySagaRepository();

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.ShouldContainSagaAsync(NewId.NextGuid(), TimeSpan.FromSeconds(1), cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("repository", exception.ParamName);
        Assert.Contains("loading or querying sagas", exception.Message, StringComparison.Ordinal);
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
}
