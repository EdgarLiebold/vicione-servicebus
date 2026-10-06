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

public sealed class ScheduledFallbackOwningDiagnosticsTests
{
    static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);
    static readonly DateTimeOffset Now = new(2044, 3, 2, 1, 2, 3, TimeSpan.Zero);
    static readonly Guid MessageId = new("6d2665b6-2047-4ba7-b734-b6a16e2decd0");
    const string Entity = "fallback-entity";
    const long Sequence = 9876543210123L;

    [Theory]
    [InlineData("past", false)]
    [InlineData("past", true)]
    [InlineData("rejected", false)]
    [InlineData("rejected", true)]
    [InlineData("not-found", false)]
    [InlineData("not-found", true)]
    [InlineData("already-cancelling", false)]
    [InlineData("already-cancelling", true)]
    [RequirementCoverage("REQ-VSB-ASB-SCHEDULER-OUTCOME", "fallback-and-idempotent-cancel-owning-debug-does-not-replace-documented-continuation")]
    public async Task SendFallbackOrIdempotentCancel_OptionalOwningDebugDoesNotChangeDocumentedContinuationAsync(string branch, bool hostile)
    {
        var previous = LogContext.Current;
        using var caller = new CancellationTokenSource();
        using var outer = new CancellationTokenSource();
        bool cancel = branch is "not-found" or "already-cancelling";
        var logger = new BranchLogger(branch, hostile);
        var sdk = new ControlledSender();
        Exception? nativeFailure = branch switch
        {
            "past" => null,
            "rejected" => new ArgumentOutOfRangeException("scheduledEnqueueTime", "unique native schedule rejection"),
            "not-found" => new ServiceBusException("unique native scheduled message absence", ServiceBusFailureReason.MessageNotFound, Entity, null),
            "already-cancelling" => new InvalidOperationException("unique native message already being cancelled"),
            _ => throw new ArgumentOutOfRangeException(nameof(branch))
        };
        var tokenSource = new AzureServiceBusSendContext<Payload>(new Payload("token source", Guid.Empty, Now), CancellationToken.None);
        tokenSource.SetScheduledMessageId(Sequence);
        Guid tokenId = tokenSource.ScheduledMessageId!.Value;
        DateTimeOffset? due = cancel ? null : branch == "past" ? Now.AddMinutes(-3) : Now.AddMinutes(17);
        var context = new AzureServiceBusSendContext<Payload>(new Payload("fallback control", cancel ? tokenId : Guid.Empty, Now), caller.Token)
        {
            Serializer = new SystemTextJsonRawMessageSerializer(JsonSerializerOptions.Default),
            DestinationAddress = new Uri("sb://unit.servicebus.invalid/" + Entity),
            MessageId = MessageId,
            ScheduledEnqueueTimeUtc = due
        };
        context.Headers.Set("workflow-marker", "fallback-continuation");
        context.GetOrAddPayload<TimeProvider>(() => new FixedClock());
        var sender = new MessageSendEndpointContext(InterfaceProxy<ConnectionContext>.Create(Unexpected), sdk);
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
            operation = transport.SendAsync(sender, context, outer.Token);
            if (branch != "past")
            {
                await Task.WhenAny(operation, sdk.PrimaryEntered.Task).WaitAsync(Bound, CancellationToken.None);
                Assert.True(sdk.PrimaryEntered.Task.IsCompletedSuccessfully);
                Assert.False(operation.IsCompleted);
                SdkCall first = Assert.Single(sdk.Calls);
                Assert.Equal(cancel ? "cancel" : "schedule", first.Operation);
                Assert.Equal(caller.Token, first.Token);
                Assert.NotEqual(outer.Token, first.Token);
                Assert.Empty(logger.Records);
                Task raw;
                if (cancel)
                {
                    Assert.Equal(Sequence, first.Sequence);
                    Assert.Null(first.Message);
                    raw = sdk.CancelRaw;
                    Assert.False(raw.IsCompleted);
                    sdk.CancelRelease.TrySetException(nativeFailure!);
                }
                else
                {
                    Assert.Equal(due, first.Due);
                    AssertMaterializedMessage(first.Message!);
                    Assert.Null(context.ScheduledMessageId);
                    raw = sdk.ScheduleRaw;
                    Assert.False(raw.IsCompleted);
                    sdk.ScheduleRelease.TrySetException(nativeFailure!);
                }
                Exception? nativeObserved = await Record.ExceptionAsync(() => raw.WaitAsync(Bound, CancellationToken.None));
                Assert.Same(nativeFailure, nativeObserved);
                Assert.True(raw.IsFaulted);
            }
            Exception? observed = null;
            if (!cancel)
            {
                // A missing fallback is detected by terminal public failure or actual native Send admission, never by a timeout-as-success.
                await Task.WhenAny(operation, sdk.SendEntered.Task).WaitAsync(Bound, CancellationToken.None);
                if (operation.IsCompleted) observed = await Record.ExceptionAsync(() => operation.WaitAsync(Bound, CancellationToken.None));
            }
            else observed = await Record.ExceptionAsync(() => operation.WaitAsync(Bound, CancellationToken.None));
            LogRecord record = Assert.Single(logger.Records);
            Assert.Equal(logger.Template, record.Fields["{OriginalFormat}"]);
            Assert.Null(record.Cause);
            Assert.Equal(hostile ? 1 : 0, logger.ThrowCount);
            if (branch == "past") Assert.Equal(due, record.Fields["DueAt"]);
            else if (branch == "rejected") Assert.Equal(MessageId, record.Fields["MessageId"]);
            else
            {
                Assert.Equal(Entity, record.Fields["DestinationAddress"]);
                Assert.Equal(tokenId, record.Fields["TokenId"]);
            }
            if (observed is not null) Assert.Same(logger.Failure, observed);
            if (!cancel)
            {
                // FIRST fallback boundary: optional Debug cannot prevent the documented actual ordinary Send admission.
                Assert.Equal(1, sdk.SendCalls);
                Assert.True(sdk.SendEntered.Task.IsCompletedSuccessfully);
                Assert.Equal(branch == "past" ? new[] { "send" } : new[] { "schedule", "send" }, sdk.Calls.Select(x => x.Operation).ToArray());
                SdkCall send = Assert.Single(sdk.Calls, x => x.Operation == "send");
                AssertMaterializedMessage(send.Message!);
                Assert.Equal(caller.Token, send.Token);
                Assert.False(sdk.SendRaw.IsCompleted);
                Assert.False(operation.IsCompleted);
                Assert.Null(context.ScheduledMessageId);
                sdk.SendRelease.TrySetResult();
                await sdk.SendRaw.WaitAsync(Bound, CancellationToken.None);
                observed = await Record.ExceptionAsync(() => operation.WaitAsync(Bound, CancellationToken.None));
                Assert.Null(observed);
            }
            else
            {
                // FIRST cancel boundary: the genuine documented native error was handled before optional Debug.
                Assert.Null(observed);
                Assert.Single(sdk.Calls);
                Assert.Equal(0, sdk.SendCalls);
            }
            Assert.True(operation.IsCompletedSuccessfully);
            Assert.Single(logger.Records);
        }
        catch (Exception exception) { primary = exception; }
        finally
        {
            try
            {
                logger.Armed = false;
                await CaptureAsync(() => { sdk.ReleaseAll(); return Task.CompletedTask; }, cleanup);
                if (operation is not null) await CaptureAsync(() => ObserveKnownAsync(operation, logger.Failure), cleanup);
                // Public wrapper first: any late fallback belongs to the final actual native-task snapshot.
                foreach (Task raw in sdk.ActualTasks.ToArray())
                    await CaptureAsync(() => ObserveKnownAsync(raw, nativeFailure), cleanup);
                await CaptureAsync(() => sdk.DisposeAsync().AsTask().WaitAsync(Bound, CancellationToken.None), cleanup);
            }
            finally { LogContext.Current = previous; }
        }
        if (cleanup.Count != 0)
        {
            if (primary is not null) cleanup.Insert(0, primary);
            throw new AggregateException("Fallback control and independent retirement failed.", cleanup);
        }
        if (primary is not null) ExceptionDispatchInfo.Capture(primary).Throw();
    }

    static void AssertMaterializedMessage(ServiceBusMessage message)
    {
        using var body = JsonDocument.Parse(message.Body.ToMemory());
        Assert.Equal("fallback control", body.RootElement.GetProperty(nameof(Payload.Value)).GetString());
        Assert.Equal("application/json", message.ContentType);
        Assert.Equal(MessageId.ToString("N"), message.MessageId);
        Assert.Equal("fallback-continuation", message.ApplicationProperties["workflow-marker"]);
    }
    static object? Unexpected(MethodInfo method, object?[]? args) => throw new NotSupportedException(method.ToString());
    static async Task CaptureAsync(Func<Task> action, List<Exception> failures)
    {
        try { await action(); }
        catch (Exception exception) { failures.Add(exception); }
    }
    static async Task ObserveKnownAsync(Task task, Exception? known)
    {
        try { await task.WaitAsync(Bound, CancellationToken.None); }
        catch (Exception exception) when (task.IsFaulted && known is not null && ReferenceEquals(exception, known)) { }
    }
    sealed record SdkCall(string Operation, ServiceBusMessage? Message, DateTimeOffset? Due, long? Sequence, CancellationToken Token);
    sealed class ControlledSender : ServiceBusSender
    {
        public readonly ConcurrentQueue<SdkCall> Calls = new();
        public readonly ConcurrentQueue<Task> ActualTasks = new();
        public readonly TaskCompletionSource PrimaryEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource SendEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource<long> ScheduleRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource CancelRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource SendRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int _sendCalls;
        public int SendCalls => Volatile.Read(ref _sendCalls);
        public Task<long> ScheduleRaw => ScheduleRelease.Task;
        public Task CancelRaw => CancelRelease.Task;
        public Task SendRaw => SendRelease.Task;
        public override string EntityPath => Entity;
        public override Task<long> ScheduleMessageAsync(ServiceBusMessage message, DateTimeOffset scheduledEnqueueTime, CancellationToken cancellationToken = default)
        {
            Calls.Enqueue(new SdkCall("schedule", message, scheduledEnqueueTime, null, cancellationToken));
            ActualTasks.Enqueue(ScheduleRaw);
            PrimaryEntered.TrySetResult();
            return ScheduleRaw;
        }
        public override Task CancelScheduledMessageAsync(long sequenceNumber, CancellationToken cancellationToken = default)
        {
            Calls.Enqueue(new SdkCall("cancel", null, null, sequenceNumber, cancellationToken));
            ActualTasks.Enqueue(CancelRaw);
            PrimaryEntered.TrySetResult();
            return CancelRaw;
        }
        public override Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default)
        {
            Calls.Enqueue(new SdkCall("send", message, null, null, cancellationToken));
            Interlocked.Increment(ref _sendCalls);
            ActualTasks.Enqueue(SendRaw);
            SendEntered.TrySetResult();
            return SendRaw;
        }
        public void ReleaseAll() { ScheduleRelease.TrySetResult(Sequence); CancelRelease.TrySetResult(); SendRelease.TrySetResult(); }
        public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
    sealed record LogRecord(Dictionary<string, object?> Fields, Exception? Cause);
    sealed class BranchLogger(string branch, bool hostile) : ILogger
    {
        public string Template => branch switch
        {
            "past" => "The scheduled time was in the past, sending: {DueAt}",
            "rejected" => "The scheduled time was rejected by the server, sending: {MessageId}",
            "not-found" => "CANCEL {DestinationAddress} {TokenId} message not found",
            "already-cancelling" => "CANCEL {DestinationAddress} {TokenId} message already being canceled",
            _ => throw new ArgumentOutOfRangeException(nameof(branch))
        };
        public readonly IOException Failure = new("unique owning fallback/idempotent Debug failure");
        public readonly ConcurrentQueue<LogRecord> Records = new();
        public volatile bool Armed = true;
        int _throws;
        public int ThrowCount => Volatile.Read(ref _throws);
        public bool IsEnabled(LogLevel level) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (level != LogLevel.Debug || state is not IEnumerable<KeyValuePair<string, object?>> fields) return;
            var values = fields.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (!values.TryGetValue("{OriginalFormat}", out var template) || !Equals(template, Template)) return;
            Records.Enqueue(new LogRecord(values, exception));
            if (Armed && hostile) { Interlocked.Increment(ref _throws); throw Failure; }
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
        protected override object? Invoke(MethodInfo? method, object?[]? args) => _handler(method ?? throw new InvalidOperationException("Missing public SPI method."), args);
    }
}
