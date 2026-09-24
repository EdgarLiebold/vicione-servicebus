using System.Text.Json;
using System.Text.Json.Nodes;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Middleware.Partitioning;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Configuration;

public sealed class JobServiceReceivePartitioningTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-ENDPOINT-CONFIGURATION", "job-receive-registration-and-cross-message-serialization")]
    public async Task JobEndpoint_SerializesTheSameJobAcrossMessageTypesWhileAnotherPartitionContinuesAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var hash = new Murmur3PartitionHashGenerator();
        Guid blockedJob = FindKeyForPartition(hash, 0);
        Guid otherJob = FindKeyForPartition(hash, 1);
        var options = new JobServiceOptions();
        var probe = new JobPartitionProbe("partitioned-job", blockedJob, otherJob);
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration =>
            configuration.ServiceInstance(instance => instance.ConfigureJobServiceEndpoints(options, context: null,
                jobService =>
                {
                    jobService.JobEndpointName = "partitioned-job";
                    jobService.ConcurrentMessageLimit = 2;
                })));
        AssertJobPartitionProbe(bus, cancellationToken);

        using ConnectHandle endpointObserver = bus.ConnectReceiveEndpointObserver(probe);
        await bus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            await probe.Ready.Task.WaitAsync(timeout, cancellationToken);
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(((IJobSagaSettings)options).JobSagaEndpointAddress,
                cancellationToken).WaitAsync(timeout, cancellationToken);

            Task first = endpoint.SendAsync<IJobSlotUnavailable>(new JobSlotUnavailable(blockedJob), cancellationToken);
            await probe.BlockedEntered.Task.WaitAsync(timeout, cancellationToken);

            Task other = endpoint.SendAsync<IJobSlotWaitElapsed>(new JobSlotWaitElapsed(otherJob), cancellationToken);
            await probe.OtherArrived.Task.WaitAsync(timeout, cancellationToken);
            await probe.OtherEntered.Task.WaitAsync(timeout, cancellationToken);

            Task second = endpoint.SendAsync<IJobSlotWaitElapsed>(new JobSlotWaitElapsed(blockedJob), cancellationToken);
            await probe.BlockedArrived.Task.WaitAsync(timeout, cancellationToken);

            Assert.False(probe.BlockedSecondEntered.Task.IsCompleted);
            probe.ReleaseBlocked.TrySetResult();

            await Task.WhenAll(first, second, other, probe.BlockedSecondEntered.Task)
                .WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            probe.ReleaseBlocked.TrySetResult();
            await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            probe.Dispose();
        }
    }

    private static void AssertJobPartitionProbe(IBusControl bus, CancellationToken cancellationToken)
    {
        JsonNode result = JsonSerializer.SerializeToNode(bus.GetProbeResult(cancellationToken).Results)!;
        JsonArray endpoints = result["bus"]!["host"]!["receiveEndpoint"]!.AsArray();
        JsonNode endpoint = Assert.Single(endpoints, node =>
            node?["name"]?.GetValue<string>() == "partitioned-job")!;
        Assert.Equal(2, endpoint["receiveTransport"]!["concurrentMessageLimit"]!.GetValue<int>());
        JsonNode deserialize = Assert.Single(endpoint["filters"]!.AsArray(), node =>
            node?["filterType"]?.GetValue<string>() == "deserialize")!;

        var partitionedTypes = new HashSet<string>(StringComparer.Ordinal);
        var coordinatorIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonNode? messagePipe in deserialize["consumePipe"]!["filters"]!.AsArray())
        {
            JsonArray? filters = messagePipe?["filters"]?.AsArray();
            if (filters == null)
                continue;

            JsonNode[] partitions = filters.Where(node =>
                node?["filterType"]?.GetValue<string>() == "partition").Select(node => node!).ToArray();
            if (partitions.Length == 0)
                continue;

            JsonNode partition = Assert.Single(partitions);
            Assert.True(partitionedTypes.Add(messagePipe!["outputType"]!.GetValue<string>()));
            JsonNode coordinator = partition["partitioner"]!;
            Assert.Equal(2, coordinator["partitionCount"]!.GetValue<int>());
            coordinatorIds.Add(coordinator["id"]!.GetValue<string>());
        }

        string[] expectedTypes =
        [
            TypeCache<ConsumeContext<IJobSubmitted>>.ShortName,
            TypeCache<ConsumeContext<IJobSlotAllocated>>.ShortName,
            TypeCache<ConsumeContext<IJobSlotUnavailable>>.ShortName,
            TypeCache<ConsumeContext<Fault<IAllocateJobSlot>>>.ShortName,
            TypeCache<ConsumeContext<Fault<IStartJobAttempt>>>.ShortName,
            TypeCache<ConsumeContext<IJobAttemptCanceled>>.ShortName,
            TypeCache<ConsumeContext<IJobAttemptCompleted>>.ShortName,
            TypeCache<ConsumeContext<IJobAttemptFaulted>>.ShortName,
            TypeCache<ConsumeContext<IJobAttemptStarted>>.ShortName,
            TypeCache<ConsumeContext<IGetJobState>>.ShortName,
            TypeCache<ConsumeContext<IJobCompleted>>.ShortName,
            TypeCache<ConsumeContext<ICancelJob>>.ShortName,
            TypeCache<ConsumeContext<IRetryJob>>.ShortName,
            TypeCache<ConsumeContext<IRunJob>>.ShortName,
            TypeCache<ConsumeContext<ISaveJobCheckpoint>>.ShortName,
            TypeCache<ConsumeContext<ISetJobProgress>>.ShortName,
            TypeCache<ConsumeContext<IJobSlotWaitElapsed>>.ShortName,
            TypeCache<ConsumeContext<IJobRetryDelayElapsed>>.ShortName,
        ];

        Assert.Equal(expectedTypes.Order(StringComparer.Ordinal), partitionedTypes.Order(StringComparer.Ordinal));
        Assert.Single(coordinatorIds);
    }

    private static Guid FindKeyForPartition(IPartitionHashGenerator hash, uint partition)
    {
        for (int value = 1; value < int.MaxValue; value++)
        {
            var key = new Guid(value, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            if (hash.ComputeHash(key.ToByteArray()) % 2 == partition)
                return key;
        }

        throw new InvalidOperationException($"No key mapped to partition {partition}.");
    }

    private sealed record JobSlotUnavailable(Guid JobId) : IJobSlotUnavailable;

    private sealed record JobSlotWaitElapsed(Guid JobId) : IJobSlotWaitElapsed;

    private sealed class JobPartitionProbe(string endpointName, Guid blockedJob, Guid otherJob) :
        IReceiveEndpointObserver,
        IConsumeMessageObserver<IJobSlotWaitElapsed>,
        IDisposable
    {
        private ConnectHandle? _firstPipe;
        private ConnectHandle? _secondPipe;
        private ConnectHandle? _arrivalObserver;

        public TaskCompletionSource Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource BlockedEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource BlockedArrived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource BlockedSecondEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource OtherArrived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource OtherEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseBlocked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task ReadyAsync(ReceiveEndpointReady ready)
        {
            if (ready.InputAddress.AbsolutePath.TrimEnd('/').Split('/')[^1] != endpointName || Ready.Task.IsCompleted)
                return Task.CompletedTask;

            _firstPipe = ready.ReceiveEndpoint.ConnectConsumePipe(
                Pipe.ExecuteAwaited<ConsumeContext<IJobSlotUnavailable>>(async context =>
                {
                    if (context.Message.JobId == blockedJob)
                    {
                        BlockedEntered.TrySetResult();
                        await ReleaseBlocked.Task;
                    }
                }));
            _secondPipe = ready.ReceiveEndpoint.ConnectConsumePipe(
                Pipe.ExecuteAwaited<ConsumeContext<IJobSlotWaitElapsed>>(context =>
                {
                    if (context.Message.JobId == blockedJob)
                        BlockedSecondEntered.TrySetResult();
                    else if (context.Message.JobId == otherJob)
                        OtherEntered.TrySetResult();

                    return Task.CompletedTask;
                }));
            _arrivalObserver = ready.ReceiveEndpoint.ConnectConsumeMessageObserver(this);
            Ready.TrySetResult();
            return Task.CompletedTask;
        }

        public Task PreConsumeAsync(ConsumeContext<IJobSlotWaitElapsed> context)
        {
            if (context.Message.JobId == blockedJob)
                BlockedArrived.TrySetResult();
            else if (context.Message.JobId == otherJob)
                OtherArrived.TrySetResult();

            return Task.CompletedTask;
        }

        public Task PostConsumeAsync(ConsumeContext<IJobSlotWaitElapsed> context) => Task.CompletedTask;

        public Task ConsumeFaultAsync(ConsumeContext<IJobSlotWaitElapsed> context, Exception exception) =>
            Task.CompletedTask;

        public Task StoppingAsync(ReceiveEndpointStopping stopping) => Task.CompletedTask;

        public Task CompletedAsync(ReceiveEndpointCompleted completed) => Task.CompletedTask;

        public Task FaultedAsync(ReceiveEndpointFaulted faulted) => Task.CompletedTask;

        public void Dispose()
        {
            _arrivalObserver?.Dispose();
            _secondPipe?.Dispose();
            _firstPipe?.Dispose();
        }
    }
}
