using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Text;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Middleware;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsReceiverFifoTests
{
    private const string QueueName = "orders.fifo";
    private const string QueueUrl = "https://sqs.eu-central-1.amazonaws.com/123456789012/orders.fifo";
    private const string QueueArn = "arn:aws:sqs:eu-central-1:123456789012:orders.fifo";
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "fifo-group-dispatch-uses-numeric-sequence-order")]
    public async Task FifoReceiver_DispatchesOneGroupInNumericSequenceOrderAsync()
    {
        Message largest = FifoMessage("largest", "group-A", "18446744073709551616");
        Message middle = FifoMessage("middle", "group-A", "10");
        Message smallest = FifoMessage("smallest", "group-A", "2");
        await using var harness = new ReceiverHarness([largest, middle, smallest], expectedDispatches: 3);

        IReadOnlyList<string> dispatched = await harness.Dispatched.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Assert.Equal(["smallest", "middle", "largest"], dispatched);
        Assert.True(harness.PollCount >= 1);
        Assert.Equal(3, harness.DispatchCount);
        Assert.True(harness.Settings.IsOrdered);
        Assert.Equal(1, harness.Settings.ConcurrentDeliveryLimit);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "fifo-groups-preserve-per-group-order-with-interleaved-batch")]
    public async Task FifoReceiver_OrdersEachInterleavedGroupIndependentlyAsync()
    {
        Message[] batch =
        [
            FifoMessage("A-high", "group-A", "10"),
            FifoMessage("B-high", "group-B", "20"),
            FifoMessage("A-low", "group-A", "2"),
            FifoMessage("B-low", "group-B", "3")
        ];
        await using var harness = new ReceiverHarness(batch, expectedDispatches: 4);

        IReadOnlyList<string> dispatched = await harness.Dispatched.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Assert.Equal(4, dispatched.Count);
        Assert.Equal(["A-low", "A-high"], dispatched.Where(id => id.StartsWith("A-", StringComparison.Ordinal)));
        Assert.Equal(["B-low", "B-high"], dispatched.Where(id => id.StartsWith("B-", StringComparison.Ordinal)));
        Assert.Equal(4, harness.DispatchCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "blocked-fifo-group-does-not-stop-other-group")]
    public async Task FifoReceiver_BlockedGroupDoesNotStopAnotherPartitionAsync()
    {
        QueueReceiveSettings settings = CreateSettings();
        const string blockedGroup = "group-A";
        var hash = new Murmur3PartitionHashGenerator();
        uint blockedPartition = hash.ComputeHash(Encoding.UTF8.GetBytes(blockedGroup)) % (uint)settings.ConcurrentMessageLimit;
        string otherGroup = Enumerable.Range(0, 100)
            .Select(index => $"group-B-{index}")
            .First(candidate => hash.ComputeHash(Encoding.UTF8.GetBytes(candidate)) % (uint)settings.ConcurrentMessageLimit != blockedPartition);
        var blockedEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseBlocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var otherGroupCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Message[] batch =
        [
            FifoMessage("A-high", blockedGroup, "10"),
            FifoMessage("B-high", otherGroup, "20"),
            FifoMessage("A-low", blockedGroup, "2"),
            FifoMessage("B-low", otherGroup, "3")
        ];
        await using var harness = new ReceiverHarness(batch, expectedDispatches: 4, settings, DispatchBehaviorAsync);

        try
        {
            await blockedEntered.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            await otherGroupCompleted.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

            Assert.False(releaseBlocked.Task.IsCompleted);
            IReadOnlyList<string> beforeRelease = harness.DispatchSnapshot;
            Assert.DoesNotContain("A-high", beforeRelease);
            Assert.Equal(["B-low", "B-high"], beforeRelease.Where(id => id.StartsWith("B-", StringComparison.Ordinal)));
        }
        finally
        {
            blockedEntered.TrySetResult();
            releaseBlocked.TrySetResult();
        }

        IReadOnlyList<string> allDispatches = await harness.Dispatched.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        Assert.Equal(["A-low", "A-high"], allDispatches.Where(id => id.StartsWith("A-", StringComparison.Ordinal)));

        async Task DispatchBehaviorAsync(string id)
        {
            if (id == "A-low")
            {
                blockedEntered.TrySetResult();
                await releaseBlocked.Task;
            }
            else if (id.StartsWith("B-", StringComparison.Ordinal))
            {
                await blockedEntered.Task;
                if (id == "B-high")
                    otherGroupCompleted.TrySetResult();
            }
        }
    }

    [Theory]
    [InlineData(null, "1", "MessageGroupId")]
    [InlineData(" ", "1", "MessageGroupId")]
    [InlineData("group-A", null, "SequenceNumber")]
    [InlineData("group-A", "invalid", "SequenceNumber")]
    [InlineData("group-A", "-1", "SequenceNumber")]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "invalid-fifo-metadata-stops-before-dispatch")]
    public async Task FifoReceiver_RejectsMissingOrInvalidOrderingAttributesBeforeDispatchAsync(
        string? group, string? sequence, string diagnostic)
    {
        ILogContext? previousLogContext = LogContext.Current;
        var logger = new CaptureLogger();
        LogContext.ConfigureCurrentLogContext(logger);
        try
        {
            await using var harness = new ReceiverHarness([FifoMessage("invalid", group, sequence)], expectedDispatches: 1);

            await harness.Receiver.Ready.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            await harness.Receiver.Completed.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

            Assert.True(harness.PollCount >= 1);
            Assert.Equal(0, harness.DispatchCount);
            Assert.True(harness.Receiver.Stopped.IsCancellationRequested);
            Assert.False(harness.Dispatched.Task.IsCompleted);
            InvalidDataException error = Assert.IsType<InvalidDataException>(Assert.Single(logger.WarningExceptions));
            Assert.Contains(diagnostic, error.Message, StringComparison.Ordinal);
        }
        finally
        {
            LogContext.Current = previousLogContext;
        }
    }

    private static Message FifoMessage(string id, string? group, string? sequence)
    {
        var attributes = new Dictionary<string, string>();
        if (group is not null)
            attributes[MessageSystemAttributeName.MessageGroupId] = group;
        if (sequence is not null)
            attributes[MessageSystemAttributeName.SequenceNumber] = sequence;
        return new Message { MessageId = id, ReceiptHandle = $"receipt-{id}", Body = "{}", Attributes = attributes };
    }

    private sealed class ReceiverHarness : IAsyncDisposable
    {
        private readonly QueueInfo _queue;
        private readonly IList<Message> _firstBatch;
        private readonly int _expectedDispatches;
        private readonly List<string> _dispatches = [];
        private int _polls;

        private readonly Func<string, Task>? _dispatchBehavior;

        public ReceiverHarness(IList<Message> firstBatch, int expectedDispatches,
            QueueReceiveSettings? settings = null, Func<string, Task>? dispatchBehavior = null)
        {
            _firstBatch = firstBatch;
            _expectedDispatches = expectedDispatches;
            _dispatchBehavior = dispatchBehavior;
            Settings = settings ?? CreateSettings();
            IAmazonSQS sqs = InterfaceProxy<IAmazonSQS>.Create((method, _) => throw new NotSupportedException(method.Name));
            _queue = new QueueInfo(QueueName, QueueUrl,
                new Dictionary<string, string> { [QueueAttributeName.QueueArn] = QueueArn },
                sqs, CancellationToken.None, true);
            ConnectionContext connection = InterfaceProxy<ConnectionContext>.Create((method, _) => throw new NotSupportedException(method.Name));
            ClientContext client = InterfaceProxy<ClientContext>.Create((method, args) => method.Name switch
            {
                nameof(PipeContext.TryGetPayload) => TryGetPayload(method, args),
                nameof(ClientContext.GetQueueInfoAsync) => Task.FromResult(_queue),
                nameof(ClientContext.ReceiveMessagesAsync) => PollAsync(args),
                "get_ConnectionContext" => connection,
                "get_CancellationToken" => CancellationToken.None,
                _ => throw new NotSupportedException(method.Name)
            });
            IReceivePipeDispatcher dispatcher = InterfaceProxy<IReceivePipeDispatcher>.Create((method, args) => method.Name switch
            {
                "add_ZeroActivity" or "remove_ZeroActivity" => null,
                "get_ActiveDispatchCount" => 0,
                "get_DispatchCount" => (long)DispatchCount,
                "get_MaxConcurrentDispatchCount" => 1,
                nameof(IReceivePipeDispatcher.DispatchAsync) => DispatchAsync(args),
                _ => throw new NotSupportedException(method.Name)
            });
            ILogContext? logContext = null;
            logContext = InterfaceProxy<ILogContext>.Create((method, _) => method.Name switch
            {
                "get_Logger" => NullLogger.Instance,
                "get_Messages" or nameof(ILogContext.CreateLogContext) => logContext,
                _ => null
            });
            SqsReceiveEndpointContext endpoint = InterfaceProxy<SqsReceiveEndpointContext>.Create((method, args) => method.Name switch
            {
                nameof(PipeContext.TryGetPayload) => NoPayload(args),
                nameof(ReceiveEndpointContext.CreateReceivePipeDispatcher) => dispatcher,
                "get_InputAddress" => new Uri("amazonsqs://eu-central-1/orders.fifo"),
                "get_LogContext" => logContext,
                "get_StopTimeout" => OperationTimeout,
                "get_ConsumerStopTimeout" => null,
                _ => throw new NotSupportedException(method.Name)
            });
            Receiver = new AmazonSqsMessageReceiver(client, endpoint);
        }

        public QueueReceiveSettings Settings { get; }
        public AmazonSqsMessageReceiver Receiver { get; }
        public TaskCompletionSource<IReadOnlyList<string>> Dispatched { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int PollCount => Volatile.Read(ref _polls);
        public int DispatchCount
        {
            get { lock (_dispatches) return _dispatches.Count; }
        }
        public IReadOnlyList<string> DispatchSnapshot
        {
            get { lock (_dispatches) return _dispatches.ToArray(); }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await Receiver.StopAsync(TestContext.Current.CancellationToken).WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            }
            finally
            {
                await _queue.DisposeAsync();
            }
        }

        private object TryGetPayload(MethodInfo method, object?[]? args)
        {
            Assert.NotNull(args);
            if (method.GetGenericArguments()[0] != typeof(ReceiveSettings))
            {
                args[0] = null;
                return false;
            }

            args[0] = Settings;
            return true;
        }

        private async Task<IList<Message>> PollAsync(object?[]? args)
        {
            Assert.NotNull(args);
            Assert.Equal(QueueName, Assert.IsType<string>(args[0]));
            int poll = Interlocked.Increment(ref _polls);
            if (poll == 1)
                return _firstBatch;

            await Task.Delay(Timeout.InfiniteTimeSpan, Assert.IsType<CancellationToken>(args[3]));
            return Array.Empty<Message>();
        }

        private Task DispatchAsync(object?[]? args)
        {
            Assert.NotNull(args);
            var context = Assert.IsType<AmazonSqsReceiveContext>(args[0]);
            string id = context.TransportMessage.MessageId;
            lock (_dispatches)
            {
                _dispatches.Add(id);
                if (_dispatches.Count == _expectedDispatches)
                    Dispatched.TrySetResult(_dispatches.ToArray());
            }
            return _dispatchBehavior?.Invoke(id) ?? Task.CompletedTask;
        }

        private static object NoPayload(object?[]? args)
        {
            Assert.NotNull(args);
            args[0] = null;
            return false;
        }
    }

    private static QueueReceiveSettings CreateSettings()
    {
        var topology = new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology());
        var parent = new AmazonSqsEndpointConfiguration(topology);
        return new QueueReceiveSettings(parent.CreateEndpointConfiguration(false), QueueName, true, false);
    }

    private sealed class CaptureLogger : ILogger
    {
        public ConcurrentQueue<Exception> WarningExceptions { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new EmptyScope();

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning && exception is not null)
                WarningExceptions.Enqueue(exception);
        }

        private sealed class EmptyScope : IDisposable
        {
            public void Dispose() { }
        }
    }
}
