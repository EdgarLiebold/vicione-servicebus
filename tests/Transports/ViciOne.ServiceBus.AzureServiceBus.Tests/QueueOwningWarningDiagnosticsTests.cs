using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class QueueOwningWarningDiagnosticsTests
{
    static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);

    public static IEnumerable<object[]> Cases =>
        from operation in new[] { "close", "dispose", "shutdown" }
        from session in new[] { false, true }
        from hostile in new[] { false, true }
        select new object[] { operation, session, hostile };

    [Theory]
    [MemberData(nameof(Cases))]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "owning-warning-does-not-replace-suppressed-sdk-failure")]
    public async Task QueueLifecycle_OptionalWarningsPreserveSuppressedSdkFailureAsync(string operation, bool session, bool hostile)
    {
        var previous = LogContext.Current;
        var input = new Uri("sb://unit.servicebus.invalid/owning-warning");
        var sdkFailure = new IOException("unique raw processor lifecycle failure");
        var logger = new WarningLogger(operation, input, sdkFailure, hostile);
        var probe = new ProcessorProbe();
        var agent = new Agent();
        agent.SetReady();
        using var caller = new CancellationTokenSource();
        ReceiveSettings settings = InterfaceProxy<ReceiveSettings>.Create((method, _) => throw new NotSupportedException(method.ToString()));
        var factories = new ConcurrentQueue<(string Method, object? Settings)>();
        var processor = new ControlledProcessor(probe);
        var sessionProcessor = new ControlledSessionProcessor(probe);
        ConnectionContext connection = InterfaceProxy<ConnectionContext>.Create((method, args) =>
        {
            if (args is not { Length: 1 })
                throw new NotSupportedException("Unexpected processor factory signature: " + method);
            factories.Enqueue((method.Name, args[0]));
            return method.Name switch
            {
                nameof(ConnectionContext.CreateQueueProcessor) => processor,
                nameof(ConnectionContext.CreateQueueSessionProcessor) => sessionProcessor,
                _ => throw new NotSupportedException(method.ToString())
            };
        });
        var context = new QueueClientContext(connection, input, settings, agent);
        Task? publicOperation = null;
        Exception? primary = null;
        var cleanup = new List<Exception>();
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            if (session)
                context.ConfigureSessionProcessor((_, _, _) => Task.CompletedTask, _ => Task.CompletedTask);
            else
                context.ConfigureMessageProcessor((_, _, _) => Task.CompletedTask, _ => Task.CompletedTask);
            var factory = Assert.Single(factories);
            Assert.Equal(session ? nameof(ConnectionContext.CreateQueueSessionProcessor) : nameof(ConnectionContext.CreateQueueProcessor), factory.Method);
            Assert.Same(settings, factory.Settings);
            Assert.Same(connection, context.ConnectionContext);
            Assert.Equal(input, context.InputAddress);
            Assert.Equal("owning-warning", context.EntityPath);
            Assert.False(context.IsClosedOrClosing);
            publicOperation = operation switch
            {
                "close" => context.CloseAsync(caller.Token),
                "dispose" => context.DisposeAsync().AsTask(),
                "shutdown" => context.ShutdownAsync(caller.Token),
                _ => throw new InvalidOperationException("Unknown lifecycle operation.")
            };
            await Task.WhenAny(publicOperation, probe.Entered.Task).WaitAsync(Bound, CancellationToken.None);
            Assert.True(probe.Entered.Task.IsCompletedSuccessfully);
            Assert.False(publicOperation.IsCompleted);
            Assert.False(probe.Raw.IsCompleted);
            var call = Assert.Single(probe.Calls);
            Assert.Equal(operation == "shutdown" ? "stop" : "close", call.Operation);
            Assert.Equal(operation == "dispose" ? CancellationToken.None : caller.Token, call.Token);
            Assert.Empty(logger.Records);
            probe.Release.TrySetException(sdkFailure);
            await ObserveKnownAsync(probe.Raw, sdkFailure);
            Assert.True(probe.Raw.IsFaulted);
            Assert.Same(sdkFailure, Assert.Single(probe.Raw.Exception!.InnerExceptions));
            Exception? observed = await Record.ExceptionAsync(() => publicOperation.WaitAsync(Bound, CancellationToken.None));
            logger.AssertRecord();
            Assert.Single(probe.Calls);
            if (observed is not null)
                Assert.Same(logger.Failure, observed);
            // FIRST causal boundary: the owning Warning must preserve the documented SDK-failure suppression.
            Assert.Null(observed);
            Assert.True(publicOperation.IsCompletedSuccessfully);
        }
        catch (Exception exception) { primary = exception; }
        finally
        {
            try
            {
                logger.Armed = false;
                await CaptureAsync(() => { probe.Release.TrySetResult(); return Task.CompletedTask; }, cleanup);
                if (publicOperation is not null)
                    await CaptureAsync(() => ObserveKnownAsync(publicOperation, logger.Failure), cleanup);
                if (probe.Entered.Task.IsCompletedSuccessfully)
                    await CaptureAsync(() => ObserveKnownAsync(probe.Raw, sdkFailure), cleanup);
                // Protected SDK mock constructors own no network client. Change only mock state after the raw task was observed.
                probe.Closed = true;
                await CaptureAsync(() => context.DisposeAsync().AsTask().WaitAsync(Bound, CancellationToken.None), cleanup);
                await CaptureAsync(() => agent.StopAsync(new FixtureStopContext(), CancellationToken.None).WaitAsync(Bound, CancellationToken.None), cleanup);
                await CaptureAsync(() => agent.Ready.WaitAsync(Bound, CancellationToken.None), cleanup);
                await CaptureAsync(() => agent.Completed.WaitAsync(Bound, CancellationToken.None), cleanup);
            }
            finally { LogContext.Current = previous; }
        }
        ThrowOutcomes(primary, cleanup);
    }

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
            throw new AggregateException("Queue control and independent fixture retirement failed.", cleanup);
        }
        if (primary is not null) ExceptionDispatchInfo.Capture(primary).Throw();
    }

    sealed class FixtureStopContext : BasePipeContext, StopContext
    {
        public string Reason => "Fixture retirement";
    }

    sealed class ProcessorProbe
    {
        public volatile bool Closed;
        public readonly ConcurrentQueue<(string Operation, CancellationToken Token)> Calls = new();
        public readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Raw => Release.Task;
        public Task InvokeAsync(string operation, CancellationToken token)
        {
            Calls.Enqueue((operation, token));
            Entered.TrySetResult();
            return Raw;
        }
    }

    sealed class ControlledProcessor(ProcessorProbe probe) : ServiceBusProcessor
    {
        public override string EntityPath => "owning-warning";
        public override bool IsClosed => probe.Closed;
        public override Task CloseAsync(CancellationToken cancellationToken = default) => probe.InvokeAsync("close", cancellationToken);
        public override Task StopProcessingAsync(CancellationToken cancellationToken = default) => probe.InvokeAsync("stop", cancellationToken);
    }

    sealed class ControlledSessionProcessor : ServiceBusSessionProcessor
    {
        readonly ProcessorProbe _probe;
        readonly ControlledProcessor _inner;
        public ControlledSessionProcessor(ProcessorProbe probe) { _probe = probe; _inner = new ControlledProcessor(probe); }
        protected override ServiceBusProcessor InnerProcessor => _inner;
        public override string EntityPath => "owning-warning";
        public override bool IsClosed => _probe.Closed;
        public override Task CloseAsync(CancellationToken cancellationToken = default) => _probe.InvokeAsync("close", cancellationToken);
        public override Task StopProcessingAsync(CancellationToken cancellationToken = default) => _probe.InvokeAsync("stop", cancellationToken);
    }

    sealed record LogRecord(Dictionary<string, object?> Fields, Exception? Cause);
    sealed class WarningLogger(string operation, Uri input, Exception sdkFailure, bool hostile) : ILogger
    {
        public readonly IOException Failure = new("unique owning queue Warning failure");
        public readonly ConcurrentQueue<LogRecord> Records = new();
        public volatile bool Armed = true;
        int _throws;
        string Template => operation == "shutdown" ? "Stop processing client faulted: {InputAddress}" : "Close client faulted: {InputAddress}";
        public bool IsEnabled(LogLevel level) => true;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new EmptyScope();
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (level != LogLevel.Warning || state is not IEnumerable<KeyValuePair<string, object?>> fields) return;
            var values = fields.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (!values.TryGetValue("{OriginalFormat}", out var template) || !Equals(template, Template)) return;
            Records.Enqueue(new LogRecord(values, exception));
            if (Armed && hostile) { Interlocked.Increment(ref _throws); throw Failure; }
        }
        public void AssertRecord()
        {
            LogRecord record = Assert.Single(Records);
            Assert.Equal(Template, record.Fields["{OriginalFormat}"]);
            Assert.Equal(input, record.Fields["InputAddress"]);
            Assert.Same(sdkFailure, record.Cause);
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
