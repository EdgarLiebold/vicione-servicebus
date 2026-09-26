using System.Collections.Concurrent;
using System.Reflection;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class ProcessorCheckpointLifecycleTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-PROCESSOR-CHECKPOINT", "canceled-event-drains-without-checkpoint-and-preserves-other-partition")]
    public async Task CanceledEvent_AllowsShutdownWithoutCheckpointAndPreservesOtherPartitionAsync()
    {
        ILogContext? previous = LogContext.Current;
        var processor = new TestProcessor();
        using var delivery = new CancellationTokenSource();
        var checkpoints = new ConcurrentQueue<(string Partition, string Offset)>();
        ProcessEventArgs canceled = Event("0", "101", _ =>
        {
            checkpoints.Enqueue(("0", "101"));
            return Task.CompletedTask;
        });
        ProcessEventArgs healthy = Event("1", "101", _ =>
        {
            checkpoints.Enqueue(("1", "101"));
            return Task.CompletedTask;
        });
        try
        {
            await processor.InitializeAsync("0");
            await processor.InitializeAsync("1");
            await processor.PendingAsync(canceled);
            await processor.PendingAsync(healthy);
            await delivery.CancelAsync();
            processor.Lock.Canceled(canceled, delivery.Token);
            await processor.Lock.CompleteAsync(healthy, TestContext.Current.CancellationToken);

            await Task.WhenAll(processor.ShutdownAsync("0"), processor.ShutdownAsync("1"))
                .WaitAsync(Timeout, TestContext.Current.CancellationToken);

            Assert.Equal([("1", "101")], checkpoints.ToArray());
            Assert.False(processor.Lifetime.IsCancellationRequested);
            await processor.AssertClientCanBeLeasedAgainAsync();
        }
        finally
        {
            try
            {
                await processor.DisposeAsync();
            }
            finally
            {
                LogContext.Current = previous;
            }
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-PROCESSOR-CHECKPOINT", "shutdown-awaits-in-flight-checkpoint-before-releasing-partition")]
    public async Task Shutdown_WaitsForInFlightCheckpointBeforeCompletingAsync()
    {
        ILogContext? previous = LogContext.Current;
        var processor = new TestProcessor();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var checkpoints = new ConcurrentQueue<(string Partition, string Offset)>();
        string? persisted = null;
        ProcessEventArgs completed = Event("0", "102", async token =>
        {
            checkpoints.Enqueue(("0", "102"));
            entered.TrySetResult();
            await release.Task.WaitAsync(token);
            persisted = "102";
        });
        try
        {
            await processor.InitializeAsync("0");
            await processor.PendingAsync(completed);
            await processor.Lock.CompleteAsync(completed, TestContext.Current.CancellationToken);
            await entered.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);

            Task closing = processor.ShutdownAsync("0");
            Assert.False(closing.IsCompleted);
            Assert.Null(persisted);
            Assert.Equal([("0", "102")], checkpoints.ToArray());

            release.SetResult();
            await closing.WaitAsync(Timeout, TestContext.Current.CancellationToken);

            Assert.Equal("102", persisted);
            Assert.Equal([("0", "102")], checkpoints.ToArray());
            Assert.False(processor.Lifetime.IsCancellationRequested);
            await processor.AssertClientCanBeLeasedAgainAsync();
        }
        finally
        {
            release.TrySetResult();
            try
            {
                await processor.DisposeAsync();
            }
            finally
            {
                LogContext.Current = previous;
            }
        }
    }

    private static ProcessEventArgs Event(string partition, string offset, Func<CancellationToken, Task> checkpoint) => new(
        EventHubsModelFactory.PartitionContext("tests.servicebus.windows.net", "orders", "group", partition),
        EventHubsModelFactory.EventData(eventBody: BinaryData.FromString($"{partition}:{offset}"), offsetString: offset),
        checkpoint, CancellationToken.None);

    private sealed class TestProcessor : IAsyncDisposable
    {
        private readonly TestEventProcessorClient _client = new();
        private readonly EventHubProcessorContext _context;
        private readonly List<string> _partitions = [];
        private readonly Dictionary<string, Task> _closures = [];

        public TestProcessor()
        {
            ILogContext? previous = LogContext.Current;
            IHostConfiguration host = DispatchProxy.Create<IHostConfiguration, HostProxy>();
            try
            {
                LogContext.ConfigureCurrentLogContext();
                ((HostProxy)host).ReceiveLogContext = LogContext.Current!;
            }
            finally
            {
                LogContext.Current = previous;
            }
            _context = new EventHubProcessorContext(host, _client, null, null, Lifetime.Token);
            Lock = new ProcessorLockContext(_context, new Settings(), Lifetime.Token);
        }

        public CancellationTokenSource Lifetime { get; } = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        public ProcessorLockContext Lock { get; }

        public async Task InitializeAsync(string partition)
        {
            await _client.InitializeAsync(partition).WaitAsync(Timeout, TestContext.Current.CancellationToken);
            _partitions.Add(partition);
        }

        public Task PendingAsync(ProcessEventArgs args) =>
            Lock.PendingAsync(args, TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);

        public Task ShutdownAsync(string partition)
        {
            if (!_closures.TryGetValue(partition, out Task? closing))
            {
                closing = _client.ShutdownAsync(partition);
                _closures.Add(partition, closing);
            }
            return closing;
        }

        public async Task AssertClientCanBeLeasedAgainAsync()
        {
            await Lock.DisposeAsync();
            await using var next = new ProcessorLockContext(_context, new Settings(), TestContext.Current.CancellationToken);
            Assert.Same(_client, next.Client);
        }

        public async ValueTask DisposeAsync()
        {
            await Lifetime.CancelAsync();
            try
            {
                await Task.WhenAll(_partitions.Select(ShutdownAsync)).WaitAsync(Timeout, CancellationToken.None);
            }
            finally
            {
                await Lock.DisposeAsync();
                Lifetime.Dispose();
            }
        }
    }

    private sealed class TestEventProcessorClient : EventProcessorClient
    {
        public Task InitializeAsync(string partition) =>
            OnInitializingPartitionAsync(new TestPartition(partition), CancellationToken.None);

        public Task ShutdownAsync(string partition) =>
            OnPartitionProcessingStoppedAsync(new TestPartition(partition), ProcessingStoppedReason.Shutdown, CancellationToken.None);
    }

    private sealed class TestPartition : EventProcessorPartition
    {
        public TestPartition(string partition) => PartitionId = partition;
    }

    private class HostProxy : DispatchProxy
    {
        public ILogContext ReceiveLogContext { get; set; } = null!;

        protected override object? Invoke(MethodInfo? method, object?[]? args) => method?.Name == "get_ReceiveLogContext"
            ? ReceiveLogContext
            : throw new NotSupportedException(method?.Name);
    }

    private sealed class Settings : ReceiveSettings
    {
        public string ConsumerGroup => "group";
        public string ContainerName => "checkpoints";
        public string EventHubName => "orders";
        public ushort CheckpointMessageLimit => 1;
        public ushort CheckpointMessageCount => 1;
        public int PrefetchCount => 1;
        public TimeSpan CheckpointInterval => TimeSpan.FromMinutes(5);
        public int ConcurrentMessageLimit => 1;
        public int ConcurrentDeliveryLimit => 1;
    }
}
