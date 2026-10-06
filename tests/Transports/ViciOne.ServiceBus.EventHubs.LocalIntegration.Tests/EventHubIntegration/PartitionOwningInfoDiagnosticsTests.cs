using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.EventHubs.Checkpoints;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class PartitionOwningInfoDiagnosticsTests
{
    static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);
    const string PartitionId = "0";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EVENTHUB-PARTITION-LIFECYCLE", "initialization-info-does-not-replace-retained-partition-admission")]
    public async Task PartitionInitialization_OptionalInfoDoesNotReplaceActualAdmissionAsync(bool hostile)
    {
        var previous = LogContext.Current;
        var logger = new InfoLogger("Partition: {PartitionId} was initialized", hostile);
        var checkpoint = new CheckpointProbe();
        ProcessorLockContext? context = null;
        Task? initialization = null;
        Task? secondInitialization = null;
        Task? admission = null;
        Task? confirmation = null;
        Task? closure = null;
        Exception? primary = null;
        var cleanup = new List<Exception>();
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            ILogContext logContext = LogContext.Current!;
            IHostConfiguration host = InterfaceProxy<IHostConfiguration>.Create((method, _) =>
                method.Name == "get_ReceiveLogContext" ? logContext : throw new NotSupportedException(method.ToString()));
            var client = new ControlledProcessorClient();
            var processor = new EventHubProcessorContext(host, client, null, null, CancellationToken.None);
            context = new ProcessorLockContext(processor, new Settings(), CancellationToken.None);
            Assert.Same(client, context.Client);
            Exception? observed = Record.Exception(() =>
            {
                initialization = context.OnPartitionInitializingAsync(InitializeArgs(), CancellationToken.None);
            });
            if (initialization is not null)
                observed = await Record.ExceptionAsync(() => initialization.WaitAsync(Bound, CancellationToken.None));
            if (observed is not null) Assert.Same(logger.Failure, observed);
            logger.AssertRecord();
            logger.Armed = false;
            secondInitialization = context.OnPartitionInitializingAsync(InitializeArgs(), CancellationToken.None);
            await secondInitialization.WaitAsync(Bound, CancellationToken.None);
            Assert.Single(logger.Records);
            ProcessEventArgs eventArgs = checkpoint.CreateEvent();
            admission = context.PendingAsync(eventArgs, CancellationToken.None);
            await admission.WaitAsync(Bound, CancellationToken.None);
            confirmation = context.CompleteAsync(eventArgs, CancellationToken.None);
            await confirmation.WaitAsync(Bound, CancellationToken.None);
            await checkpoint.Entered.Task.WaitAsync(Bound, CancellationToken.None);
            Assert.Equal(1, checkpoint.Calls);
            Assert.False(checkpoint.Raw.IsCompleted);
            Assert.True(checkpoint.WorkerToken.CanBeCanceled);
            closure = context.OnPartitionClosingAsync(CloseArgs(), CancellationToken.None);
            Assert.False(closure.IsCompleted);
            checkpoint.Release.TrySetResult();
            await checkpoint.Raw.WaitAsync(Bound, CancellationToken.None);
            await closure.WaitAsync(Bound, CancellationToken.None);
            // FIRST causal boundary: the partition was actually admitted and drained; its optional Info cannot fail admission.
            Assert.Null(observed);
            Assert.NotNull(initialization);
            Assert.True(initialization.IsCompletedSuccessfully);
            Assert.True(admission.IsCompletedSuccessfully);
            Assert.True(confirmation.IsCompletedSuccessfully);
            Assert.True(closure.IsCompletedSuccessfully);
            Assert.Equal(1, checkpoint.Calls);
        }
        catch (Exception exception) { primary = exception; }
        finally
        {
            try
            {
                logger.Armed = false;
                await CaptureAsync(() => { checkpoint.Release.TrySetResult(); return Task.CompletedTask; }, cleanup);
                if (context is not null)
                {
                    await CaptureAsync(() => context.CompleteAsync(checkpoint.CreateEvent(), CancellationToken.None).WaitAsync(Bound, CancellationToken.None), cleanup);
                    if (closure is null)
                        await CaptureAsync(() => { closure = context.OnPartitionClosingAsync(CloseArgs(), CancellationToken.None); return Task.CompletedTask; }, cleanup);
                }
                foreach (Task? operation in new[] { initialization, secondInitialization, admission, confirmation, closure })
                    if (operation is not null) await CaptureAsync(() => operation.WaitAsync(Bound, CancellationToken.None), cleanup);
                if (checkpoint.Entered.Task.IsCompletedSuccessfully)
                    await CaptureAsync(() => checkpoint.Raw.WaitAsync(Bound, CancellationToken.None), cleanup);
                if (context is not null)
                    await CaptureAsync(() => context.DisposeAsync().AsTask().WaitAsync(Bound, CancellationToken.None), cleanup);
            }
            finally { LogContext.Current = previous; }
        }
        ThrowOutcomes(primary, cleanup);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EVENTHUB-PARTITION-LIFECYCLE", "closure-info-does-not-skip-partition-owner-token-retirement")]
    public async Task PartitionClose_OptionalInfoDoesNotSkipOwnerTokenRetirementAsync(bool hostile)
    {
        var previous = LogContext.Current;
        var logger = new InfoLogger("Partition: {PartitionId} was closed, reason: {Reason}", hostile);
        var checkpoint = new CheckpointProbe();
        var pending = new PendingConfirmationCollection(CancellationToken.None);
        var partition = new PartitionCheckpointData(new Settings(), pending);
        Task? admission = null;
        Task? closure = null;
        Exception? primary = null;
        var cleanup = new List<Exception>();
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            ProcessEventArgs eventArgs = checkpoint.CreateEvent();
            admission = partition.PendingAsync(eventArgs, CancellationToken.None);
            await admission.WaitAsync(Bound, CancellationToken.None);
            pending.Complete(eventArgs);
            await checkpoint.Entered.Task.WaitAsync(Bound, CancellationToken.None);
            Assert.Equal(1, checkpoint.Calls);
            Assert.True(checkpoint.WorkerToken.CanBeCanceled);
            Assert.False(checkpoint.Raw.IsCompleted);
            closure = partition.CloseAsync(CloseArgs(), CancellationToken.None);
            Assert.False(closure.IsCompleted);
            Assert.Empty(logger.Records);
            checkpoint.Release.TrySetResult();
            await checkpoint.Raw.WaitAsync(Bound, CancellationToken.None);
            Exception? observed = await Record.ExceptionAsync(() => closure.WaitAsync(Bound, CancellationToken.None));
            logger.AssertRecord();
            Assert.Equal(1, checkpoint.Calls);
            Assert.True(checkpoint.Raw.IsCompletedSuccessfully);
            if (observed is not null) Assert.Same(logger.Failure, observed);
            // FIRST causal boundary: optional Info after the actual checkpoint drain cannot prevent owner retirement.
            Assert.Null(observed);
            Assert.True(closure.IsCompletedSuccessfully);
            // Observe the borrowed token; never dispose its WaitHandle from the fixture.
            Exception? tokenFailure = Record.Exception(() => { _ = checkpoint.WorkerToken.WaitHandle; });
            Assert.IsType<ObjectDisposedException>(tokenFailure);
        }
        catch (Exception exception) { primary = exception; }
        finally
        {
            try
            {
                logger.Armed = false;
                await CaptureAsync(() => { pending.Complete(checkpoint.CreateEvent()); return Task.CompletedTask; }, cleanup);
                await CaptureAsync(() => { checkpoint.Release.TrySetResult(); return Task.CompletedTask; }, cleanup);
                if (admission is not null) await CaptureAsync(() => admission.WaitAsync(Bound, CancellationToken.None), cleanup);
                if (closure is not null) await CaptureAsync(() => ObserveKnownAsync(closure, logger.Failure), cleanup);
                if (closure is null || !closure.IsCompletedSuccessfully)
                    await CaptureAsync(() => partition.CloseAsync(CloseArgs(), CancellationToken.None).WaitAsync(Bound, CancellationToken.None), cleanup);
                if (checkpoint.Entered.Task.IsCompletedSuccessfully)
                    await CaptureAsync(() => checkpoint.Raw.WaitAsync(Bound, CancellationToken.None), cleanup);
                await CaptureAsync(() => { pending.Dispose(); return Task.CompletedTask; }, cleanup);
            }
            finally { LogContext.Current = previous; }
        }
        ThrowOutcomes(primary, cleanup);
    }

    static PartitionInitializingEventArgs InitializeArgs() => new(PartitionId, EventPosition.Earliest, CancellationToken.None);
    static PartitionClosingEventArgs CloseArgs() => new(PartitionId, ProcessingStoppedReason.Shutdown, CancellationToken.None);

    static async Task CaptureAsync(Func<Task> action, List<Exception> failures)
    {
        try { await action(); }
        catch (Exception exception) { failures.Add(exception); }
    }

    static async Task ObserveKnownAsync(Task task, Exception known)
    {
        try { await task.WaitAsync(Bound, CancellationToken.None); }
        catch (Exception exception) when (task.IsFaulted && ReferenceEquals(exception, known)) { }
    }

    static void ThrowOutcomes(Exception? primary, List<Exception> cleanup)
    {
        if (cleanup.Count != 0)
        {
            if (primary is not null) cleanup.Insert(0, primary);
            throw new AggregateException("Partition control and independent retirement failed.", cleanup);
        }
        if (primary is not null) ExceptionDispatchInfo.Capture(primary).Throw();
    }

    sealed class ControlledProcessorClient : EventProcessorClient { }

    sealed class CheckpointProbe
    {
        public readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int _calls;
        public int Calls => Volatile.Read(ref _calls);
        public CancellationToken WorkerToken;
        public Task Raw => Release.Task;
        public ProcessEventArgs CreateEvent() => new(
            EventHubsModelFactory.PartitionContext("tests.servicebus.windows.net", "orders", "group", PartitionId),
            EventHubsModelFactory.EventData(eventBody: BinaryData.FromString("checkpoint control"), offsetString: "101"),
            token =>
            {
                WorkerToken = token;
                Interlocked.Increment(ref _calls);
                Entered.TrySetResult();
                return Raw;
            }, CancellationToken.None);
    }

    sealed class Settings : ReceiveSettings
    {
        public string ConsumerGroup => "group";
        public string ContainerName => "checkpoints";
        public string EventHubName => "orders";
        public ushort CheckpointMessageLimit => 1;
        public ushort CheckpointMessageCount => 1;
        public int PrefetchCount => 3;
        public TimeSpan CheckpointInterval => TimeSpan.FromMinutes(30);
        public int ConcurrentMessageLimit => 1;
        public int ConcurrentDeliveryLimit => 1;
    }

    sealed record LogRecord(Dictionary<string, object?> Fields, Exception? Cause);
    sealed class InfoLogger(string template, bool hostile) : ILogger
    {
        public readonly IOException Failure = new("unique owning partition Info failure");
        public readonly ConcurrentQueue<LogRecord> Records = new();
        public volatile bool Armed = true;
        int _throws;
        public bool IsEnabled(LogLevel level) => true;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new EmptyScope();
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (level != LogLevel.Information || state is not IEnumerable<KeyValuePair<string, object?>> fields) return;
            var values = fields.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (!values.TryGetValue("{OriginalFormat}", out var value) || !Equals(value, template)) return;
            Records.Enqueue(new LogRecord(values, exception));
            if (Armed && hostile) { Interlocked.Increment(ref _throws); throw Failure; }
        }
        public void AssertRecord()
        {
            LogRecord record = Assert.Single(Records);
            Assert.Equal(template, record.Fields["{OriginalFormat}"]);
            Assert.Equal(PartitionId, record.Fields["PartitionId"]);
            if (template.Contains("{Reason}", StringComparison.Ordinal)) Assert.Equal(ProcessingStoppedReason.Shutdown, record.Fields["Reason"]);
            Assert.Null(record.Cause);
            Assert.Equal(hostile ? 1 : 0, Volatile.Read(ref _throws));
        }
        sealed class EmptyScope : IDisposable { public void Dispose() { } }
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
            _handler(method ?? throw new InvalidOperationException("Missing public interface method."), args);
    }
}
