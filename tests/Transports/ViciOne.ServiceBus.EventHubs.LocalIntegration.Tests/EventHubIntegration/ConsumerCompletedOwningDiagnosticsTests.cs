using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.EventHubs.Middleware;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class ConsumerCompletedOwningDiagnosticsTests
{
    static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);
    static readonly Uri InputAddress = new("sb://unit.servicebus.invalid/orders");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EVENTHUB-RECEIVE-ADMISSION", "completed-receiver-owning-debug-does-not-replace-processor-pipeline-continuation")]
    public async Task SendAsync_OptionalCompletedDebugDoesNotReplaceActualReceiverShutdownAsync(bool hostile)
    {
        var previous = LogContext.Current;
        var logger = new CompletedLogger(hostile);
        var client = new ControlledProcessorClient();
        var observers = new ReceiveTransportObservable();
        var observer = new LifecycleObserver();
        ConnectHandle observerHandle = observers.Connect(observer);
        var captured = new TaskCompletionSource<IAgent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var next = new NextPipe();
        EventHubProcessorContext? processor = null;
        IAgent? receiver = null;
        Task? operation = null;
        Task? stop = null;
        Exception? primary = null;
        var cleanup = new List<Exception>();
        int admissions = 0;
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            ILogContext log = LogContext.Current!;
            IHostConfiguration host = InterfaceProxy<IHostConfiguration>.Create((method, _) =>
                method.Name == "get_ReceiveLogContext" ? log : throw new NotSupportedException(method.ToString()));
            processor = new EventHubProcessorContext(host, client, null, null, CancellationToken.None);
            IReceivePipeDispatcher dispatcher = InterfaceProxy<IReceivePipeDispatcher>.Create((method, _) => method.Name switch
            {
                "add_ZeroActivity" or "remove_ZeroActivity" => null,
                "get_ActiveDispatchCount" or "get_MaxConcurrentDispatchCount" => 0,
                "get_DispatchCount" => 0L,
                _ => throw new NotSupportedException(method.ToString())
            });
            var settings = new Settings();
            ReceiveEndpointContext endpoint = InterfaceProxy<ReceiveEndpointContext>.Create((method, args) =>
            {
                if (method.Name == "TryGetPayload" && method.IsGenericMethod && method.GetGenericArguments().Single() == typeof(ReceiveSettings))
                {
                    args![0] = settings;
                    return true;
                }
                if (method.Name == "TryGetPayload" && method.IsGenericMethod && method.GetGenericArguments().Single() == typeof(TimeProvider))
                {
                    args![0] = null;
                    return false;
                }
                if (method.Name == "AddConsumeAgent")
                {
                    IAgent actual = (IAgent)args![0]!;
                    Interlocked.Increment(ref admissions);
                    if (!captured.TrySetResult(actual)) throw new InvalidOperationException("Duplicate receiver admission.");
                    return null;
                }
                return method.Name switch
                {
                    "CreateReceivePipeDispatcher" => dispatcher,
                    "get_InputAddress" => InputAddress,
                    "get_LogContext" => log,
                    "get_TransportObservers" => observers,
                    "get_ConsumerStopTimeout" or "get_StopTimeout" => null,
                    _ => throw new NotSupportedException(method.ToString())
                };
            });
            operation = new EventHubConsumerFilter(endpoint).SendAsync(processor, next);
            await Task.WhenAny(operation, client.StartEntered.Task).WaitAsync(Bound, CancellationToken.None);
            Assert.True(client.StartEntered.Task.IsCompletedSuccessfully);
            Assert.Equal(1, client.StartCalls);
            Assert.True(client.StartToken.CanBeCanceled);
            Assert.False(client.StartRaw.IsCompleted);
            client.StartRelease.TrySetResult();
            await client.StartRaw.WaitAsync(Bound, CancellationToken.None);
            await Task.WhenAny(operation, captured.Task).WaitAsync(Bound, CancellationToken.None);
            Assert.True(captured.Task.IsCompletedSuccessfully);
            receiver = await captured.Task.WaitAsync(Bound, CancellationToken.None);
            Assert.IsType<EventHubDataReceiver>(receiver);
            await receiver.Ready.WaitAsync(Bound, CancellationToken.None);
            await observer.ReadyEntered.Task.WaitAsync(Bound, CancellationToken.None);
            Assert.Equal(1, Volatile.Read(ref admissions));
            Assert.False(operation.IsCompleted);
            Assert.Empty(logger.Records);
            stop = receiver.StopAsync("Public consumer completion control", CancellationToken.None);
            await Task.WhenAny(stop, client.StopEntered.Task).WaitAsync(Bound, CancellationToken.None);
            Assert.True(client.StopEntered.Task.IsCompletedSuccessfully);
            Assert.Equal(1, client.StopCalls);
            Assert.Equal(CancellationToken.None, client.StopToken);
            Assert.False(client.StopRaw.IsCompleted);
            client.StopRelease.TrySetResult();
            await client.StopRaw.WaitAsync(Bound, CancellationToken.None);
            await stop.WaitAsync(Bound, CancellationToken.None);
            await receiver.Completed.WaitAsync(Bound, CancellationToken.None);
            Exception? observed = await Record.ExceptionAsync(() => operation.WaitAsync(Bound, CancellationToken.None));
            Assert.True(stop.IsCompletedSuccessfully);
            Assert.True(receiver.Completed.IsCompletedSuccessfully);
            Assert.True(client.StartRaw.IsCompletedSuccessfully);
            Assert.True(client.StopRaw.IsCompletedSuccessfully);
            Assert.Equal(1, client.StartCalls);
            Assert.Equal(1, client.StopCalls);
            ReceiveTransportReady ready = Assert.Single(observer.ReadyRecords);
            ReceiveTransportCompleted completed = Assert.Single(observer.CompletedRecords);
            Assert.Equal(InputAddress, ready.InputAddress);
            Assert.True(ready.IsStarted);
            Assert.Equal(InputAddress, completed.InputAddress);
            Assert.Equal(0L, completed.DeliveryCount);
            Assert.Equal(0, completed.MaxConcurrentDeliveryCount);
            LogRecord record = Assert.Single(logger.Records);
            Assert.Equal(CompletedLogger.Template, record.Fields["{OriginalFormat}"]);
            Assert.Equal(InputAddress, record.Fields["InputAddress"]);
            Assert.Equal(0L, record.Fields["DeliveryCount"]);
            Assert.Equal(0, record.Fields["MaxConcurrentDeliveryCount"]);
            Assert.Null(record.Cause);
            Assert.Equal(hostile ? 1 : 0, logger.ThrowCount);
            if (observed is not null) Assert.Same(logger.Failure, observed);
            // FIRST causal boundary: the real receiver and SDK stop succeeded before this optional diagnostic.
            Assert.Null(observed);
            Assert.True(operation.IsCompletedSuccessfully);
            Assert.Equal(1, next.Calls);
            Assert.Same(processor, next.Context);
            Assert.Same(client, processor.GetClient(new EmptyBuilder()));
            processor.ReleaseClient();
        }
        catch (Exception exception) { primary = exception; }
        finally
        {
            try
            {
                logger.Armed = false;
                await CaptureAsync(() => { client.StartRelease.TrySetResult(); client.StopRelease.TrySetResult(); return Task.CompletedTask; }, cleanup);
                if (receiver is null && operation is not null)
                {
                    await CaptureAsync(async () =>
                    {
                        await Task.WhenAny(operation, captured.Task).WaitAsync(Bound, CancellationToken.None);
                        if (captured.Task.IsCompletedSuccessfully)
                            receiver = await captured.Task.WaitAsync(Bound, CancellationToken.None);
                    }, cleanup);
                }
                if (receiver is not null && stop is null)
                    await CaptureAsync(() => { stop = receiver.StopAsync("Independent receiver retirement", CancellationToken.None); return Task.CompletedTask; }, cleanup);
                if (receiver is not null)
                {
                    await CaptureAsync(() => receiver.Ready.WaitAsync(Bound, CancellationToken.None), cleanup);
                    if (stop is not null) await CaptureAsync(() => stop.WaitAsync(Bound, CancellationToken.None), cleanup);
                    await CaptureAsync(() => receiver.Completed.WaitAsync(Bound, CancellationToken.None), cleanup);
                }
                if (operation is not null) await CaptureAsync(() => ObserveKnownAsync(operation, logger.Failure), cleanup);
                // The public wrapper is observed before the final actual native-task snapshot.
                foreach (Task raw in client.ActualTasks.ToArray())
                    await CaptureAsync(() => raw.WaitAsync(Bound, CancellationToken.None), cleanup);
                if (processor is not null) await CaptureAsync(() => { processor.ReleaseClient(); return Task.CompletedTask; }, cleanup);
                await CaptureAsync(() => { observerHandle.Disconnect(); return Task.CompletedTask; }, cleanup);
                await CaptureAsync(() => { observerHandle.Dispose(); return Task.CompletedTask; }, cleanup);
            }
            finally { LogContext.Current = previous; }
        }
        if (cleanup.Count != 0)
        {
            if (primary is not null) cleanup.Insert(0, primary);
            throw new AggregateException("Consumer completion and independent retirement failed.", cleanup);
        }
        if (primary is not null) ExceptionDispatchInfo.Capture(primary).Throw();
    }

    static async Task CaptureAsync(Func<Task> action, List<Exception> errors)
    {
        try { await action(); }
        catch (Exception exception) { errors.Add(exception); }
    }
    static async Task ObserveKnownAsync(Task task, Exception known)
    {
        try { await task.WaitAsync(Bound, CancellationToken.None); }
        catch (Exception exception) when (task.IsFaulted && ReferenceEquals(exception, known)) { }
    }
    sealed class ControlledProcessorClient : EventProcessorClient
    {
        public readonly TaskCompletionSource StartEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource StopEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource StartRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource StopRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly ConcurrentQueue<Task> ActualTasks = new();
        int _startCalls, _stopCalls;
        public int StartCalls => Volatile.Read(ref _startCalls);
        public int StopCalls => Volatile.Read(ref _stopCalls);
        public CancellationToken StartToken, StopToken;
        public Task StartRaw => StartRelease.Task;
        public Task StopRaw => StopRelease.Task;
        public override Task StartProcessingAsync(CancellationToken cancellationToken = default)
        {
            StartToken = cancellationToken;
            Interlocked.Increment(ref _startCalls);
            ActualTasks.Enqueue(StartRaw);
            StartEntered.TrySetResult();
            return StartRaw;
        }
        public override Task StopProcessingAsync(CancellationToken cancellationToken = default)
        {
            StopToken = cancellationToken;
            Interlocked.Increment(ref _stopCalls);
            ActualTasks.Enqueue(StopRaw);
            StopEntered.TrySetResult();
            return StopRaw;
        }
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
    sealed class LifecycleObserver : IReceiveTransportObserver
    {
        public readonly TaskCompletionSource ReadyEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly ConcurrentQueue<ReceiveTransportReady> ReadyRecords = new();
        public readonly ConcurrentQueue<ReceiveTransportCompleted> CompletedRecords = new();
        public Task ReadyAsync(ReceiveTransportReady ready) { ReadyRecords.Enqueue(ready); ReadyEntered.TrySetResult(); return Task.CompletedTask; }
        public Task CompletedAsync(ReceiveTransportCompleted completed) { CompletedRecords.Enqueue(completed); return Task.CompletedTask; }
        public Task FaultedAsync(ReceiveTransportFaulted faulted) => throw new InvalidOperationException("Unexpected transport fault notification.", faulted.Exception);
    }
    sealed class NextPipe : IPipe<ProcessorContext>
    {
        int _calls;
        public int Calls => Volatile.Read(ref _calls);
        public ProcessorContext? Context;
        public Task SendAsync(ProcessorContext context) { Context = context; Interlocked.Increment(ref _calls); return Task.CompletedTask; }
        public void Probe(ProbeContext context) { }
    }
    sealed class EmptyBuilder : ProcessorClientBuilderContext
    {
        public Task OnPartitionInitializingAsync(PartitionInitializingEventArgs eventArgs, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task OnPartitionClosingAsync(PartitionClosingEventArgs eventArgs, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
    sealed record LogRecord(Dictionary<string, object?> Fields, Exception? Cause);
    sealed class CompletedLogger(bool hostile) : ILogger
    {
        public const string Template = "Consumer Completed: {InputAddress}: {DeliveryCount} received, {MaxConcurrentDeliveryCount} concurrent peak";
        public readonly IOException Failure = new("unique owning consumer-completed Debug failure");
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
