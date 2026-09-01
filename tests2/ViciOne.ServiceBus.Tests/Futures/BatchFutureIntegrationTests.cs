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
    public async Task AllSuccessfulJobs_CompleteWithEveryProcessedJobInInputOrder()
    {
        await using BatchFutureFixture fixture = await BatchFutureFixture.Start();
        string[] jobs = ["C12345", "C54321"];

        Response<BatchCompleted> response = await fixture.Client.GetResponse<BatchCompleted>(
            new BatchRequestMessage(NewId.NextGuid(), null, jobs),
            fixture.CancellationToken).WaitAsync(fixture.Timeout, fixture.CancellationToken);

        Assert.Equal(jobs, response.Message.ProcessedJobsNumbers);
        Assert.Single(fixture.Harness.Sent.Select<BatchCompleted>(SnapshotOnlyToken()));
        Assert.Empty(fixture.Harness.Sent.Select<BatchFaulted>(SnapshotOnlyToken()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-BATCH-FUTURE", "partial-failure-reports-only-successful-jobs")]
    public async Task PartiallyFaultedBatch_ReturnsOnlyTheSuccessfulJobNumbersExactlyOnce()
    {
        await using BatchFutureFixture fixture = await BatchFutureFixture.Start();
        string[] jobs = ["C12345", "Error", "C54321", "Error", "C33454"];

        Response<BatchCompleted, BatchFaulted> response = await fixture.Client.GetResponse<BatchCompleted, BatchFaulted>(
            new BatchRequestMessage(NewId.NextGuid(), null, jobs),
            fixture.CancellationToken).WaitAsync(fixture.Timeout, fixture.CancellationToken);

        Assert.True(response.Is(out Response<BatchFaulted>? faulted));
        Assert.NotNull(faulted);
        Assert.Equal(["C12345", "C54321", "C33454"], faulted.Message.ProcessedJobsNumbers);
        Assert.Single(fixture.Harness.Sent.Select<BatchFaulted>(SnapshotOnlyToken()));
        Assert.Empty(fixture.Harness.Sent.Select<BatchCompleted>(SnapshotOnlyToken()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-BATCH-FUTURE", "held-job-prevents-terminal-response-until-release")]
    public async Task HeldJob_CompletesOnlyAfterTheTestOwnedReleaseAndPreservesResultOrder()
    {
        var observation = new BatchWorkObservation();
        await using BatchFutureFixture fixture = await BatchFutureFixture.Start(observation);
        string[] jobs = ["C12345", "Delay"];
        Task<Response<BatchCompleted>> pending = fixture.Client.GetResponse<BatchCompleted>(
            new BatchRequestMessage(NewId.NextGuid(), null, jobs),
            fixture.CancellationToken);

        await observation.DelayedEntered.Task.WaitAsync(fixture.Timeout, fixture.CancellationToken);
        Assert.False(pending.IsCompleted);
        observation.ReleaseDelayed.TrySetResult();
        Response<BatchCompleted> response = await pending.WaitAsync(fixture.Timeout, fixture.CancellationToken);

        Assert.Equal(jobs, response.Message.ProcessedJobsNumbers);
        Assert.Equal(1, observation.DelayedCount);
        Assert.Single(fixture.Harness.Sent.Select<BatchCompleted>(SnapshotOnlyToken()));
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
            BatchRequest command = context.GetCommand<BatchRequest>();
            List<string> processed = context.SelectResults<ProcessBatchItemCompleted>()
                .Select(result => result.JobNumber)
                .ToList();
            return new
            {
                command.CorrelationId,
                ProcessedJobsNumbers = processed,
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

        public void EnterDelayed()
        {
            Interlocked.Increment(ref _delayedCount);
            DelayedEntered.TrySetResult();
        }
    }

    public sealed class ProcessBatchItemConsumer(BatchWorkObservation observation) : IConsumer<ProcessBatchItem>
    {
        public async Task Consume(ConsumeContext<ProcessBatchItem> context)
        {
            if (context.Message.JobNumber == "Error")
                throw new ExpectedBatchFailure();
            if (context.Message.JobNumber == "Delay")
            {
                observation.EnterDelayed();
                await observation.ReleaseDelayed.Task.WaitAsync(context.CancellationToken);
            }

            await context.RespondAsync<ProcessBatchItemCompleted>(new
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

        private BatchFutureFixture(ServiceProvider provider, ITestHarness harness, TimeSpan timeout)
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

        public static async Task<BatchFutureFixture> Start(BatchWorkObservation? observation = null)
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
                ITestHarness harness = await provider.StartTestHarness()
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
                await Harness.Stop(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
            }
            finally
            {
                await _provider.DisposeAsync();
            }
        }
    }
}
