using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Futures;

public sealed class BatchFutureIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-BATCH-FUTURE", "all-success-results-preserve-input-order")]
    public async Task AllSuccessfulJobs_CompleteWithEveryProcessedJobInInputOrderAsync()
    {
        await using BatchFutureFixture fixture = await BatchFutureFixture.StartAsync();
        string[] jobs = ["C12345", "C54321"];

        Response<BatchCompleted> response = await fixture.Client.Advanced().GetResponseAsync<BatchCompleted>(
            new BatchRequestMessage(NewId.NextGuid(), null, jobs),
            cancellationToken: fixture.CancellationToken).WaitAsync(fixture.Timeout, fixture.CancellationToken);

        Assert.Equal(jobs, response.Message.ProcessedJobsNumbers);
        Assert.Single(fixture.Harness.Sent.Select<BatchCompleted>(SnapshotOnlyToken()));
        Assert.Empty(fixture.Harness.Sent.Select<BatchFaulted>(SnapshotOnlyToken()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-BATCH-FUTURE", "partial-failure-reports-only-successful-jobs")]
    public async Task PartiallyFaultedBatch_ReturnsOnlyTheSuccessfulJobNumbersExactlyOnceAsync()
    {
        await using BatchFutureFixture fixture = await BatchFutureFixture.StartAsync();
        string[] jobs = ["C12345", "Error", "C54321", "Error", "C33454"];

        Response<BatchCompleted, BatchFaulted> response = await fixture.Client.Advanced().GetResponseAsync<BatchCompleted, BatchFaulted>(
            new BatchRequestMessage(NewId.NextGuid(), null, jobs),
            cancellationToken: fixture.CancellationToken).WaitAsync(fixture.Timeout, fixture.CancellationToken);

        Assert.True(response.Is(out Response<BatchFaulted>? faulted));
        Assert.NotNull(faulted);
        Assert.Equal(["C12345", "C54321", "C33454"], faulted.Message.ProcessedJobsNumbers);
        Assert.Single(fixture.Harness.Sent.Select<BatchFaulted>(SnapshotOnlyToken()));
        Assert.Empty(fixture.Harness.Sent.Select<BatchCompleted>(SnapshotOnlyToken()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-BATCH-FUTURE", "held-job-prevents-terminal-response-until-release")]
    public async Task HeldJob_CompletesOnlyAfterTheTestOwnedReleaseAndPreservesResultOrderAsync()
    {
        var observation = new BatchWorkObservation();
        await using BatchFutureFixture fixture = await BatchFutureFixture.StartAsync(observation);
        string[] jobs = ["C12345", "Delay"];
        Task<Response<BatchCompleted>> pending = fixture.Client.Advanced().GetResponseAsync<BatchCompleted>(
            new BatchRequestMessage(NewId.NextGuid(), null, jobs),
            cancellationToken: fixture.CancellationToken);

        await observation.DelayedEntered.Task.WaitAsync(fixture.Timeout, fixture.CancellationToken);
        Assert.False(pending.IsCompleted);
        observation.ReleaseDelayed.TrySetResult();
        Response<BatchCompleted> response = await pending.WaitAsync(fixture.Timeout, fixture.CancellationToken);

        Assert.Equal(jobs, response.Message.ProcessedJobsNumbers);
        Assert.Equal(1, observation.DelayedCount);
        Assert.Single(fixture.Harness.Sent.Select<BatchCompleted>(SnapshotOnlyToken()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-BATCH-FUTURE", "completed-result-is-durable-without-reexecuting-fanout")]
    public async Task CompletedFuture_ReplaysTheDurableResultWithoutRepeatingAnyChildRequestAsync()
    {
        var observation = new BatchWorkObservation();
        await using BatchFutureFixture fixture = await BatchFutureFixture.StartAsync(observation);
        Guid correlationId = NewId.NextGuid();
        string[] jobs = ["First", "Second", "Third"];
        var command = new BatchRequestMessage(correlationId, null, jobs);

        Response<BatchCompleted> first = await fixture.Client.Advanced().GetResponseAsync<BatchCompleted>(
            command,
            cancellationToken: fixture.CancellationToken).WaitAsync(fixture.Timeout, fixture.CancellationToken);
        Response<BatchCompleted> replay = await fixture.Client.Advanced().GetResponseAsync<BatchCompleted>(
            command,
            cancellationToken: fixture.CancellationToken).WaitAsync(fixture.Timeout, fixture.CancellationToken);

        Assert.Equal(jobs, first.Message.ProcessedJobsNumbers);
        Assert.Equal(jobs, replay.Message.ProcessedJobsNumbers);
        Assert.Equal(2, fixture.Harness.Sent.Select<BatchCompleted>(SnapshotOnlyToken()).Count());
        Assert.Equal(jobs, observation.JobAttempts.Keys.Order());
        Assert.All(observation.JobAttempts.Values, count => Assert.Equal(1, count));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-BATCH-FUTURE", "faulted-result-is-durable-without-reexecuting-fanout")]
    public async Task FaultedFuture_ReplaysTheDurableFaultWithoutRepeatingAnyChildRequestAsync()
    {
        var observation = new BatchWorkObservation();
        await using BatchFutureFixture fixture = await BatchFutureFixture.StartAsync(observation);
        Guid correlationId = NewId.NextGuid();
        string[] jobs = ["First", "Error", "Third"];
        var command = new BatchRequestMessage(correlationId, null, jobs);

        Response<BatchCompleted, BatchFaulted> first = await fixture.Client.Advanced().GetResponseAsync<BatchCompleted, BatchFaulted>(
            command,
            cancellationToken: fixture.CancellationToken).WaitAsync(fixture.Timeout, fixture.CancellationToken);
        Response<BatchCompleted, BatchFaulted> replay = await fixture.Client.Advanced().GetResponseAsync<BatchCompleted, BatchFaulted>(
            command,
            cancellationToken: fixture.CancellationToken).WaitAsync(fixture.Timeout, fixture.CancellationToken);

        Assert.True(first.Is(out Response<BatchFaulted>? firstFault));
        Assert.True(replay.Is(out Response<BatchFaulted>? replayFault));
        Assert.NotNull(firstFault);
        Assert.NotNull(replayFault);
        Assert.Equal(["First", "Third"], firstFault.Message.ProcessedJobsNumbers);
        Assert.Equal(firstFault.Message.ProcessedJobsNumbers, replayFault.Message.ProcessedJobsNumbers);
        Assert.Equal(2, fixture.Harness.Sent.Select<BatchFaulted>(SnapshotOnlyToken()).Count());
        Assert.Equal(jobs.Order(), observation.JobAttempts.Keys.Order());
        Assert.All(observation.JobAttempts.Values, count => Assert.Equal(1, count));
    }

    private static CancellationToken SnapshotOnlyToken() => new(canceled: true);

    public interface BatchRequest : CorrelatedBy<Guid>
    {
        DateTime? BatchExpiry { get; }

        IReadOnlyList<string> JobNumbers { get; }
    }

    public sealed record BatchRequestMessage(
        Guid CorrelationId,
        DateTime? BatchExpiry,
        IReadOnlyList<string> JobNumbers) : BatchRequest;

    public interface BatchCompleted
    {
        Guid CorrelationId { get; }

        IReadOnlyList<string> ProcessedJobsNumbers { get; }
    }

    public interface BatchFaulted
    {
        Guid CorrelationId { get; }

        IReadOnlyList<string> ProcessedJobsNumbers { get; }
    }

    public interface ProcessBatchItem : CorrelatedBy<Guid>
    {
        string JobNumber { get; }
    }

    public interface ProcessBatchItemCompleted : CorrelatedBy<Guid>
    {
        string JobNumber { get; }
    }

    public sealed class BatchFuture : Future<BatchRequest, BatchCompleted, BatchFaulted>
    {
        public BatchFuture()
        {
            ConfigureCommand(configuration =>
                configuration.CorrelateById(context => context.Message.CorrelationId));
            SendRequests<string, ProcessBatchItem>(request => request.JobNumbers, configuration =>
                {
                    configuration.UsingRequestInitializer(context => new
                    {
                        CorrelationId = NewId.NextGuid(),
                        JobNumber = context.Message,
                    });
                    configuration.TrackPendingRequest(message => message.CorrelationId);
                })
                .OnResponseReceived<ProcessBatchItemCompleted>(configuration =>
                    configuration.CompletePendingRequest(message => message.CorrelationId));
            WhenAllCompleted(response => response.SetCompletedUsingInitializer(MapResponse));
            WhenAllCompletedOrFaulted(response => response.SetFaultedUsingInitializer(MapResponse));
        }

        private static object MapResponse(BehaviorContext<FutureState> context)
        {
            BatchRequest command = context.GetCommand<BatchRequest>()
                ?? throw new Xunit.Sdk.XunitException("Expected the future batch command to be available.");
            HashSet<string> processed = context.SelectResults<ProcessBatchItemCompleted>()
                .Select(result => result.JobNumber)
                .ToHashSet(StringComparer.Ordinal);
            return new
            {
                command.CorrelationId,
                ProcessedJobsNumbers = command.JobNumbers.Where(processed.Contains).ToList(),
            };
        }
    }

    public sealed class BatchWorkObservation
    {
        private int _delayedCount;

        public int DelayedCount => Volatile.Read(ref _delayedCount);

        public TaskCompletionSource DelayedEntered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseDelayed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public System.Collections.Concurrent.ConcurrentDictionary<string, int> JobAttempts { get; } =
            new(StringComparer.Ordinal);

        public void EnterDelayed()
        {
            Interlocked.Increment(ref _delayedCount);
            DelayedEntered.TrySetResult();
        }

        public void Record(string jobNumber) =>
            JobAttempts.AddOrUpdate(jobNumber, 1, static (_, count) => count + 1);

    }

    public sealed class ProcessBatchItemConsumer(BatchWorkObservation observation) : IConsumer<ProcessBatchItem>
    {
        public async Task ConsumeAsync(ConsumeContext<ProcessBatchItem> context)
        {
            observation.Record(context.Message.JobNumber);
            if (context.Message.JobNumber == "Error")
                throw new ExpectedBatchFailure();
            if (context.Message.JobNumber == "Delay")
            {
                observation.EnterDelayed();
                await observation.ReleaseDelayed.Task.WaitAsync(context.CancellationToken);
            }

            await context.Advanced().RespondAsync<ProcessBatchItemCompleted>(new
            {
                context.Message.CorrelationId,
                context.Message.JobNumber,
            });
        }
    }

    public sealed class ExpectedBatchFailure : Exception;

    private sealed class BatchFutureFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;

        private BatchFutureFixture(
            ServiceProvider provider,
            ITestHarness harness,
            TimeSpan timeout)
        {
            _provider = provider;
            Harness = harness;
            Timeout = timeout;
            Client = harness.GetRequestClient<BatchRequest>();
        }

        public CancellationToken CancellationToken => TestContext.Current.CancellationToken;

        public IRequestClient<BatchRequest> Client { get; }

        public ITestHarness Harness { get; }

        public TimeSpan Timeout { get; }

        public static async Task<BatchFutureFixture> StartAsync(BatchWorkObservation? observation = null)
        {
            TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
                .GetValidatedOptions().OperationTimeout!.Value;
            observation ??= new BatchWorkObservation();
            ServiceProvider provider = new ServiceCollection()
                .AddSingleton(observation)
                .AddViciOneServiceBusTestHarness(configuration =>
                {
                    configuration.SetTestTimeouts(timeout, timeout);
                    configuration.AddConsumer<ProcessBatchItemConsumer>();
                    configuration.AddFuture<BatchFuture>();
                })
                .BuildServiceProvider(validateScopes: true);
            try
            {
                ITestHarness harness = await provider.StartTestHarnessAsync()
                    .WaitAsync(timeout, TestContext.Current.CancellationToken);
                return new BatchFutureFixture(provider, harness, timeout);
            }
            catch
            {
                await provider.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await Harness.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
            }
            finally
            {
                await _provider.DisposeAsync();
            }
        }
    }
}
