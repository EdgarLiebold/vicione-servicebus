using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ScheduledOwningDiagnosticsTests
{
    static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);
    const long Sequence = 9876543210123L;
    const string Entity = "scheduled-entity";
    static readonly DateTimeOffset Now = new(2044, 3, 2, 1, 2, 3, TimeSpan.Zero);
    static readonly DateTimeOffset Due = Now.AddMinutes(17);
    static readonly Guid MessageId = new("1e92c4d6-4bd2-4d58-9753-3d2fa6e5b704");
    static readonly Guid CorrelationId = new("ff058c0c-ac39-4734-a143-679915252dfe");

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [RequirementCoverage("REQ-VSB-ASB-SCHEDULER-OUTCOME", "accepted-sdk-schedule-or-cancel-is-not-replaced-or-duplicated-by-owning-diagnostic")]
    public async Task SendScheduleOrCancel_OptionalOwningDiagnosticDoesNotReplaceAcceptedSdkOutcomeAsync(bool cancel, int loggerMode)
    {
        var previous = LogContext.Current;
        using var caller = new CancellationTokenSource();
        using var outer = new CancellationTokenSource();
        var logger = new SelectedLogger(cancel, loggerMode);
        var sdk = new ControlledSender();
        ConnectionContext connection = InterfaceProxy<ConnectionContext>.Create(Unexpected);
        var sender = new MessageSendEndpointContext(connection, sdk);
        var source = new AzureServiceBusSendContext<Payload>(new Payload("portable token source", Guid.Empty, Now), CancellationToken.None);
        source.SetScheduledMessageId(Sequence);
        Guid tokenId = source.ScheduledMessageId!.Value;
        var context = new AzureServiceBusSendContext<Payload>(new Payload("scheduled payload", cancel ? tokenId : Guid.Empty, Now), caller.Token)
        {
            Serializer = new SystemTextJsonRawMessageSerializer(JsonSerializerOptions.Default),
            DestinationAddress = new Uri("sb://unit.servicebus.invalid/" + Entity),
            MessageId = MessageId,
            CorrelationId = CorrelationId,
            ScheduledEnqueueTimeUtc = cancel ? null : Due
        };
        context.Headers.Set("workflow-marker", "owning-diagnostic-control");
        context.GetOrAddPayload<TimeProvider>(() => new FixedClock());
        ISerialization serialization = new SerializationConfiguration().CreateSerializerCollection();
        ReceiveEndpointContext endpoint = InterfaceProxy<ReceiveEndpointContext>.Create((method, _) =>
            method.Name == "get_Serialization" ? serialization : throw new NotSupportedException(method.ToString()));
        var transport = new ServiceBusSendTransportContext(InterfaceProxy<IServiceBusHostConfiguration>.Create(Unexpected), endpoint,
            InterfaceProxy<ISendEndpointContextSupervisor>.Create(Unexpected), new QueueSendSettings(new CreateQueueOptions(Entity)));
        Task? operation = null;
        Exception? primary = null;
        var cleanup = new List<Exception>();
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            Assert.Equal(Entity, sender.EntityPath);
            Assert.Same(connection, sender.ConnectionContext);
            operation = transport.SendAsync(sender, context, outer.Token);
            await Task.WhenAny(operation, sdk.FirstEntered.Task).WaitAsync(Bound, CancellationToken.None);
            Assert.True(sdk.FirstEntered.Task.IsCompletedSuccessfully);
            Assert.False(operation.IsCompleted);
            var first = Assert.Single(sdk.Calls);
            Assert.Equal(cancel ? "cancel" : "schedule", first.Operation);
            Assert.Equal(caller.Token, first.Token);
            Assert.NotEqual(outer.Token, first.Token);
            Assert.Empty(logger.Records);
            if (cancel)
            {
                Assert.Equal(Sequence, first.Sequence);
                Assert.Null(first.Message);
                Assert.False(sdk.CancelRaw.IsCompleted);
                sdk.CancelRelease.TrySetResult();
                await sdk.CancelRaw.WaitAsync(Bound, CancellationToken.None);
            }
            else
            {
                Assert.Equal(Due, first.Due);
                Assert.NotNull(first.Message);
                AssertMaterializedMessage(first.Message);
                Assert.Null(context.ScheduledMessageId);
                Assert.False(sdk.ScheduleRaw.IsCompleted);
                sdk.ScheduleRelease.TrySetResult(Sequence);
                Assert.Equal(Sequence, await sdk.ScheduleRaw.WaitAsync(Bound, CancellationToken.None));
            }
            Exception? observed = null;
            if (!cancel && loggerMode == 2)
            {
                // A committed extra Send is a finite failure boundary, without completing that unwanted SDK operation.
                await Task.WhenAny(operation, sdk.SendEntered.Task).WaitAsync(Bound, CancellationToken.None);
                if (operation.IsCompleted)
                    observed = await Record.ExceptionAsync(() => operation.WaitAsync(Bound, CancellationToken.None));
            }
            else
                observed = await Record.ExceptionAsync(() => operation.WaitAsync(Bound, CancellationToken.None));
            var record = Assert.Single(logger.Records);
            Assert.Equal(logger.Template, record.Fields["{OriginalFormat}"]);
            Assert.Null(record.Cause);
            Assert.Equal(loggerMode == 0 ? 0 : 1, logger.ThrowCount);
            if (cancel)
            {
                Assert.Equal(Entity, record.Fields["DestinationAddress"]);
                Assert.Equal(tokenId, record.Fields["TokenId"]);
                Assert.True(sdk.CancelRaw.IsCompletedSuccessfully);
            }
            else
            {
                Assert.True(context.TryGetScheduledMessageId(out long actualSequence));
                Assert.Equal(Sequence, actualSequence);
                Assert.True(context.ScheduledMessageId.HasValue);
                Assert.Equal(context.DestinationAddress, record.Fields["DestinationAddress"]);
                Assert.Equal(MessageId, record.Fields["MessageId"]);
                Assert.Contains(nameof(Payload), Assert.IsType<string>(record.Fields["MessageType"]), StringComparison.Ordinal);
                Assert.Equal(Due, record.Fields["DeliveryTime"]);
                Assert.Equal(context.ScheduledMessageId.Value, record.Fields["Token"]);
                Assert.True(sdk.ScheduleRaw.IsCompletedSuccessfully);
            }
            if (observed is not null) Assert.Same(logger.Failure, observed);
            if (!cancel && loggerMode == 2)
            {
                // FIRST AORE causal boundary: owning diagnostics cannot duplicate an accepted schedule as an ordinary send.
                Assert.Single(sdk.Calls);
                Assert.Null(observed);
            }
            else
            {
                // FIRST IO/healthy causal boundary: the owning diagnostic cannot replace the successful SDK operation.
                Assert.Null(observed);
            }
            await operation.WaitAsync(Bound, CancellationToken.None);
            Assert.True(operation.IsCompletedSuccessfully);
            Assert.Single(sdk.Calls);
            Assert.False(sdk.SendEntered.Task.IsCompleted);
        }
        catch (Exception exception) { primary = exception; }
        finally
        {
            try
            {
                logger.Armed = false;
                await CaptureAsync(() => { sdk.ReleaseAll(); return Task.CompletedTask; }, cleanup);
                if (operation is not null) await CaptureAsync(() => ObserveKnownAsync(operation, logger.Failure), cleanup);
                // Join the public wrapper first, then the final actual SDK snapshot: a late fallback cannot escape.
                foreach (Task raw in sdk.ActualTasks.ToArray())
                    await CaptureAsync(() => raw.WaitAsync(Bound, CancellationToken.None), cleanup);
                await CaptureAsync(() => sdk.DisposeAsync().AsTask().WaitAsync(Bound, CancellationToken.None), cleanup);
            }
            finally { LogContext.Current = previous; }
        }
        if (cleanup.Count != 0)
        {
            if (primary is not null) cleanup.Insert(0, primary);
            throw new AggregateException("Scheduled operation control and independent retirement failed.", cleanup);
        }
        if (primary is not null) ExceptionDispatchInfo.Capture(primary).Throw();
    }

    static void AssertMaterializedMessage(ServiceBusMessage message)
    {
        using var body = JsonDocument.Parse(message.Body.ToMemory());
        Assert.Equal("scheduled payload", body.RootElement.GetProperty(nameof(Payload.Value)).GetString());
        Assert.Equal("application/json", message.ContentType);
        Assert.Equal(MessageId.ToString("N"), message.MessageId);
        Assert.Equal(CorrelationId.ToString("N"), message.CorrelationId);
        Assert.Equal("owning-diagnostic-control", message.ApplicationProperties["workflow-marker"]);
    }

    static object? Unexpected(MethodInfo method, object?[]? args) => throw new NotSupportedException(method.ToString());
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

    sealed record SdkCall(string Operation, ServiceBusMessage? Message, DateTimeOffset? Due, long? Sequence, CancellationToken Token);
    sealed class ControlledSender : ServiceBusSender
    {
        public readonly ConcurrentQueue<SdkCall> Calls = new();
        public readonly ConcurrentQueue<Task> ActualTasks = new();
        public readonly TaskCompletionSource FirstEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource SendEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource<long> ScheduleRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource CancelRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource SendRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<long> ScheduleRaw => ScheduleRelease.Task;
        public Task CancelRaw => CancelRelease.Task;
        public override string EntityPath => Entity;
        public override Task<long> ScheduleMessageAsync(ServiceBusMessage message, DateTimeOffset scheduledEnqueueTime, CancellationToken cancellationToken = default)
        {
            Calls.Enqueue(new SdkCall("schedule", message, scheduledEnqueueTime, null, cancellationToken));
            ActualTasks.Enqueue(ScheduleRaw);
            FirstEntered.TrySetResult();
            return ScheduleRaw;
        }
        public override Task CancelScheduledMessageAsync(long sequenceNumber, CancellationToken cancellationToken = default)
        {
            Calls.Enqueue(new SdkCall("cancel", null, null, sequenceNumber, cancellationToken));
            ActualTasks.Enqueue(CancelRaw);
            FirstEntered.TrySetResult();
            return CancelRaw;
        }
        public override Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default)
        {
            Calls.Enqueue(new SdkCall("send", message, null, null, cancellationToken));
            ActualTasks.Enqueue(SendRelease.Task);
            SendEntered.TrySetResult();
            return SendRelease.Task;
        }
        public void ReleaseAll()
        {
            ScheduleRelease.TrySetResult(Sequence);
            CancelRelease.TrySetResult();
            SendRelease.TrySetResult();
        }
        public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    sealed record LogRecord(Dictionary<string, object?> Fields, Exception? Cause);
    sealed class SelectedLogger(bool cancel, int mode) : ILogger
    {
        public string Template => cancel ? "CANCEL {DestinationAddress} {TokenId}" : "SCHED {DestinationAddress} {MessageId} {MessageType} {DeliveryTime:G} {Token}";
        public readonly Exception Failure = mode == 2
            ? new ArgumentOutOfRangeException("owningDiagnostic", "unique owning SCHED diagnostic AORE")
            : new IOException("unique owning scheduled-operation diagnostic IO");
        public readonly ConcurrentQueue<LogRecord> Records = new();
        public volatile bool Armed = true;
        int _throws;
        public int ThrowCount => Volatile.Read(ref _throws);
        public bool IsEnabled(LogLevel level) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? error, Func<TState, Exception?, string> formatter)
        {
            if (level != LogLevel.Debug || state is not IEnumerable<KeyValuePair<string, object?>> fields) return;
            var values = fields.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (!values.TryGetValue("{OriginalFormat}", out var value) || !Equals(value, Template)) return;
            Records.Enqueue(new LogRecord(values, error));
            if (Armed && mode != 0) { Interlocked.Increment(ref _throws); throw Failure; }
        }
    }

    sealed class FixedClock : TimeProvider { public override DateTimeOffset GetUtcNow() => Now; }
    public sealed record Payload(string Value, Guid TokenId, DateTimeOffset Timestamp) : CancelScheduledMessage;
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
