using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs.Processor;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.EventHubs.Checkpoints;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class CheckpointSelectedClockTests
{
    static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);
    static readonly TimeSpan Interval = TimeSpan.FromSeconds(7);

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-PROCESSOR-CHECKPOINT", "endpoint-clock-reaches-partition-batch-and-expires-partial-checkpoint")]
    public async Task EndpointClock_ReachesCheckpointTimerAndExpiresPartialBatchAsync()
    {
        var clock = new RecordingClock();
        await WithReceiverAsync(1, clock, async owner =>
        {
            var checkpoint = new CheckpointGate("full-unique-101");
            await owner.QueueCompletedAsync(checkpoint);
            await checkpoint.Entered.Task.WaitAsync(Bound, CancellationToken.None);
            // FIRST finite oracle: an actual full-batch SDK checkpoint follows timer construction.
            TimerRecord timer = Assert.Single(clock.Timers.ToArray());
            Assert.Equal(Interval, timer.DueTime);
            Assert.Equal(Timeout.InfiniteTimeSpan, timer.Period);
            Assert.Equal("full-unique-101", checkpoint.Offset);
            Assert.True(checkpoint.Token.CanBeCanceled);
            Assert.False(checkpoint.Token.IsCancellationRequested);
        });

        // A separate real owner prevents full-batch state from supplying partial-batch evidence.
        var partialClock = new RecordingClock();
        await WithReceiverAsync(2, partialClock, async owner =>
        {
            var checkpoint = new CheckpointGate("partial-unique-202");
            await owner.QueueCompletedAsync(checkpoint);
            await partialClock.Created.Task.WaitAsync(Bound, CancellationToken.None);
            TimerRecord timer = Assert.Single(partialClock.Timers.ToArray());
            Assert.Equal(Interval, timer.DueTime);
            Assert.Equal(Timeout.InfiniteTimeSpan, timer.Period);
            // Snapshot only: timer creation alone does not prove a quiescent scheduler or a race guarantee.
            Assert.False(checkpoint.Entered.Task.IsCompleted);
            partialClock.Advance(Interval);
            await checkpoint.Entered.Task.WaitAsync(Bound, CancellationToken.None);
            Assert.Equal("partial-unique-202", checkpoint.Offset);
            Assert.Equal(1, checkpoint.Calls);
            Assert.True(checkpoint.Token.CanBeCanceled);
            // Expiry cancels the batching wait, never the SDK checkpoint owner's token.
            Assert.False(checkpoint.Token.IsCancellationRequested);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-PROCESSOR-CHECKPOINT", "legacy-batch-constructor-keeps-full-batch-checkpoint-and-drain")]
    public async Task LegacyBatchConstructor_FullBatchCheckpointsAndDrainsAsync()
    {
        var checkpoint = new CheckpointGate("legacy-unique-303");
        using var lifetime = new CancellationTokenSource();
        var checkpointer = new BatchCheckpointer(new Settings(1), lifetime.Token);
        var confirmation = new PendingConfirmation(checkpoint.Args);
        var actual = new List<Task>();
        Exception? primary = null;
        var errors = new List<Exception>();
        try
        {
            Task queued = checkpointer.PendingAsync(confirmation, TestContext.Current.CancellationToken);
            actual.Add(queued);
            await queued.WaitAsync(Bound, CancellationToken.None);
            confirmation.Complete();
            await checkpoint.Entered.Task.WaitAsync(Bound, CancellationToken.None);
            Assert.Equal(lifetime.Token, checkpoint.Token);
            Assert.Equal(1, checkpoint.Calls);
            Assert.False(checkpoint.Token.IsCancellationRequested);
        }
        catch (Exception exception) { primary = exception; }
        finally
        {
            checkpoint.Release.TrySetResult();
            confirmation.Complete();
            foreach (Task task in actual.Concat(checkpoint.ActualTasks.ToArray()))
                await CaptureAsync(() => task.WaitAsync(Bound, CancellationToken.None), errors);
            await CaptureAsync(() => checkpointer.DisposeAsync().AsTask().WaitAsync(Bound, CancellationToken.None), errors);
        }
        Rethrow(primary, errors);
    }

    static async Task WithReceiverAsync(ushort count, RecordingClock clock, Func<ReceiverOwner, Task> body)
    {
        var previous = LogContext.Current;
        ReceiverOwner? owner = null;
        Exception? primary = null;
        var errors = new List<Exception>();
        try
        {
            LogContext.ConfigureCurrentLogContext(NullLogger.Instance);
            owner = new ReceiverOwner(count, clock, LogContext.Current!);
            await owner.StartAndInitializeAsync();
            await body(owner);
        }
        catch (Exception exception) { primary = exception; }
        finally
        {
            try
            {
                if (owner is not null) await CaptureAsync(owner.CleanupAsync, errors);
            }
            finally { LogContext.Current = previous; }
        }
        Rethrow(primary, errors);
    }

    static async Task CaptureAsync(Func<Task> action, List<Exception> errors)
    {
        try { await action(); }
        catch (Exception exception) { errors.Add(exception); }
    }

    static void Rethrow(Exception? primary, List<Exception> errors)
    {
        if (errors.Count != 0)
        {
            if (primary is not null) errors.Insert(0, primary);
            throw new AggregateException("Checkpoint clock oracle and independent cleanup failed.", errors);
        }
        if (primary is not null) ExceptionDispatchInfo.Capture(primary).Throw();
    }

    sealed class ReceiverOwner
    {
        readonly ControlledClient _client = new();
        readonly EventHubProcessorContext _inner;
        readonly EventHubDataReceiver _receiver;
        readonly List<Task> _actual = [];
        readonly List<CheckpointGate> _checkpoints = [];
        ProcessorLockContext? _receipt;
        Task? _close, _stop;
        bool _initializationAttempted;

        public ReceiverOwner(ushort count, RecordingClock clock, ILogContext log)
        {
            IHostConfiguration host = InterfaceProxy<IHostConfiguration>.Create((method, _) =>
                method.Name == "get_ReceiveLogContext" ? log : throw new NotSupportedException(method.ToString()));
            _inner = new EventHubProcessorContext(host, _client, null, null, CancellationToken.None);
            ProcessorContext forwarding = InterfaceProxy<ProcessorContext>.Create((method, args) =>
            {
                if (method.Name == "GetClient")
                {
                    _receipt = Assert.IsType<ProcessorLockContext>(args![0]);
                    return _inner.GetClient(_receipt);
                }
                if (method.Name == "ReleaseClient") { _inner.ReleaseClient(); return null; }
                if (method.Name == "get_LogContext") return _inner.LogContext;
                throw new NotSupportedException(method.ToString());
            });
            IReceivePipeDispatcher dispatcher = InterfaceProxy<IReceivePipeDispatcher>.Create((method, _) => method.Name switch
            {
                "add_ZeroActivity" or "remove_ZeroActivity" => null,
                "get_ActiveDispatchCount" or "get_MaxConcurrentDispatchCount" => 0,
                "get_DispatchCount" => 0L,
                _ => throw new NotSupportedException(method.ToString())
            });
            ReceiveEndpointContext endpoint = InterfaceProxy<ReceiveEndpointContext>.Create((method, args) =>
            {
                if (method.Name == "TryGetPayload" && method.IsGenericMethod && method.GetGenericArguments()[0] == typeof(TimeProvider))
                { args![0] = clock; return true; }
                return method.Name switch
                {
                    "CreateReceivePipeDispatcher" => dispatcher,
                    "get_InputAddress" => new Uri("sb://unit.servicebus.invalid/clock"),
                    "get_LogContext" => log,
                    "get_ConsumerStopTimeout" or "get_StopTimeout" => null,
                    _ => throw new NotSupportedException(method.ToString())
                };
            });
            _receiver = new EventHubDataReceiver(new Settings(count), endpoint, forwarding);
        }

        public async Task StartAndInitializeAsync()
        {
            await _receiver.Ready.WaitAsync(Bound, CancellationToken.None);
            Assert.NotNull(_receipt);
            Assert.Same(_client, _receipt.Client);
            _initializationAttempted = true;
            Task initialize = _client.InitializeAsync();
            _actual.Add(initialize);
            await initialize.WaitAsync(Bound, CancellationToken.None);
        }

        public async Task QueueCompletedAsync(CheckpointGate checkpoint)
        {
            _checkpoints.Add(checkpoint);
            Task pending = _receipt!.PendingAsync(checkpoint.Args, TestContext.Current.CancellationToken);
            _actual.Add(pending);
            await pending.WaitAsync(Bound, CancellationToken.None);
            Task completed = _receipt.CompleteAsync(checkpoint.Args, TestContext.Current.CancellationToken);
            _actual.Add(completed);
            await completed.WaitAsync(Bound, CancellationToken.None);
        }

        public async Task CleanupAsync()
        {
            var errors = new List<Exception>();
            // Release all provider gates before any partition close/public stop can wait on them.
            foreach (CheckpointGate checkpoint in _checkpoints) checkpoint.Release.TrySetResult();
            // End any confirmation whose caller was canceled between pending admission and completion.
            if (_receipt is not null)
                foreach (CheckpointGate checkpoint in _checkpoints)
                    await CaptureAsync(() =>
                    { _receipt.Canceled(checkpoint.Args, CancellationToken.None); return Task.CompletedTask; }, errors);
            foreach (Task task in _actual.Concat(_checkpoints.SelectMany(x => x.ActualTasks.ToArray())).ToArray())
                await CaptureAsync(() => task.WaitAsync(Bound, CancellationToken.None), errors);
            if (_initializationAttempted)
                await CaptureAsync(async () =>
                {
                    _close ??= _client.CloseAsync();
                    await _close.WaitAsync(Bound, CancellationToken.None);
                }, errors);
            await CaptureAsync(async () =>
            {
                _stop ??= _receiver.StopAsync("Checkpoint clock independent cleanup", CancellationToken.None);
                await _stop.WaitAsync(Bound, CancellationToken.None);
            }, errors);
            await CaptureAsync(() => _receiver.Completed.WaitAsync(Bound, CancellationToken.None), errors);
            await CaptureAsync(() => _receiver.Ready.WaitAsync(Bound, CancellationToken.None), errors);
            if (_close is not null)
                await CaptureAsync(() => _close.WaitAsync(Bound, CancellationToken.None), errors);
            foreach (Task task in _client.ActualTasks.ToArray())
                await CaptureAsync(() => task.WaitAsync(Bound, CancellationToken.None), errors);
            // Public Stop owns receipt retirement. If it failed, independently release the actual lease/registration.
            if (_stop?.IsCompletedSuccessfully != true && _receipt is not null)
                await CaptureAsync(() => _receipt.DisposeAsync().AsTask().WaitAsync(Bound, CancellationToken.None), errors);
            Rethrow(null, errors);
        }
    }

    sealed class CheckpointGate
    {
        int _calls;
        public CheckpointGate(string offset)
        {
            Offset = offset;
            Args = new ProcessEventArgs(
                EventHubsModelFactory.PartitionContext("tests.servicebus.windows.net", "clock", "group", "0"),
                EventHubsModelFactory.EventData(eventBody: BinaryData.FromString(offset), offsetString: offset),
                CheckpointAsync, CancellationToken.None);
        }
        public string Offset { get; }
        public ProcessEventArgs Args { get; }
        public CancellationToken Token { get; private set; }
        public int Calls => Volatile.Read(ref _calls);
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ConcurrentQueue<Task> ActualTasks { get; } = new();
        Task CheckpointAsync(CancellationToken token)
        {
            Token = token;
            Interlocked.Increment(ref _calls);
            Task actual = Release.Task.WaitAsync(token);
            ActualTasks.Enqueue(actual);
            Entered.TrySetResult();
            return actual;
        }
    }

    sealed class ControlledClient : EventProcessorClient
    {
        public ConcurrentQueue<Task> ActualTasks { get; } = new();
        public override Task StartProcessingAsync(CancellationToken cancellationToken = default)
        { ActualTasks.Enqueue(Task.CompletedTask); return Task.CompletedTask; }
        public override Task StopProcessingAsync(CancellationToken cancellationToken = default)
        { ActualTasks.Enqueue(Task.CompletedTask); return Task.CompletedTask; }
        public Task InitializeAsync() => OnInitializingPartitionAsync(new TestPartition(), CancellationToken.None);
        public Task CloseAsync() => OnPartitionProcessingStoppedAsync(new TestPartition(), ProcessingStoppedReason.Shutdown, CancellationToken.None);
    }
    sealed class TestPartition : EventProcessorPartition
    {
        public TestPartition() => PartitionId = "0";
    }
    sealed record TimerRecord(TimeSpan DueTime, TimeSpan Period);
    sealed class RecordingClock : TimeProvider
    {
        readonly FakeTimeProvider _inner = new();
        public ConcurrentQueue<TimerRecord> Timers { get; } = new();
        public TaskCompletionSource Created { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override DateTimeOffset GetUtcNow() => _inner.GetUtcNow();
        public override long GetTimestamp() => _inner.GetTimestamp();
        public override long TimestampFrequency => _inner.TimestampFrequency;
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            ITimer timer = _inner.CreateTimer(callback, state, dueTime, period);
            Timers.Enqueue(new TimerRecord(dueTime, period));
            Created.TrySetResult();
            return timer;
        }
        public void Advance(TimeSpan interval) => _inner.Advance(interval);
    }
    sealed class Settings(ushort count) : ReceiveSettings
    {
        public string ConsumerGroup => "group";
        public string ContainerName => "checkpoints";
        public string EventHubName => "clock";
        public ushort CheckpointMessageLimit => 2;
        public ushort CheckpointMessageCount => count;
        public int PrefetchCount => 3;
        public TimeSpan CheckpointInterval => Interval;
        public int ConcurrentMessageLimit => 1;
        public int ConcurrentDeliveryLimit => 1;
    }
    public class InterfaceProxy<T> : DispatchProxy where T : class
    {
        Func<MethodInfo, object?[]?, object?> _handler = null!;
        public static T Create(Func<MethodInfo, object?[]?, object?> handler)
        {
            T value = Create<T, InterfaceProxy<T>>();
            ((InterfaceProxy<T>)(object)value)._handler = handler;
            return value;
        }
        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            _handler(method ?? throw new InvalidOperationException("Missing public SPI method."), args);
    }
}
