using System.Collections.Concurrent;
using System.Reflection;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class QueueProcessorLifecycleTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    public static IEnumerable<object[]> Cases =>
        from session in new[] { false, true }
        from operation in new[] { "start", "stop", "close", "dispose" }
        from fault in new[] { false, true }
        select new object[] { session, operation, fault };

    [Theory]
    [MemberData(nameof(Cases))]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "queue-sdk-lifecycle-awaits-and-preserves-failure-contract")]
    public async Task QueueLifecycle_AwaitsActualSdkAndPreservesStartFaultOrStopCloseWarningAsync(bool session, string operation, bool fault)
    {
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var probe = new ProcessorProbe();
        var processor = new RecordingProcessor(probe);
        var sessionProcessor = new RecordingSessionProcessor(probe);
        ReceiveSettings settings = DispatchProxy.Create<ReceiveSettings, RejectCalls>();
        ConnectionContext connection = DispatchProxy.Create<ConnectionContext, RecordingConnection>();
        var connectionRecorder = (RecordingConnection)connection;
        connectionRecorder.Settings = settings;
        connectionRecorder.Processor = processor;
        connectionRecorder.SessionProcessor = sessionProcessor;
        var input = new Uri("sb://unit.servicebus.invalid/queue-lifecycle");
        var context = new QueueClientContext(connection, input, settings, null!);
        if (session)
            context.ConfigureSessionProcessor((_, _, _) => Task.CompletedTask, _ => Task.CompletedTask);
        else
            context.ConfigureMessageProcessor((_, _, _) => Task.CompletedTask, _ => Task.CompletedTask);

        var logger = new RecordingLogger();
        var failure = new ExpectedLifecycleException();
        ILogContext? previous = LogContext.Current;
        Task? pending = null;
        try
        {
            Assert.Same(connection, context.ConnectionContext);
            Assert.Equal(input, context.InputAddress);
            Assert.Equal("queue-lifecycle", context.EntityPath);
            Assert.False(context.IsClosedOrClosing);
            FactoryCall factory = Assert.Single(connectionRecorder.Calls);
            Assert.Equal(session ? nameof(ConnectionContext.CreateQueueSessionProcessor) : nameof(ConnectionContext.CreateQueueProcessor), factory.Method);
            Assert.Same(settings, factory.Settings);
            LogContext.ConfigureCurrentLogContext(logger);
            pending = operation switch
            {
                "start" => context.StartAsync(caller.Token),
                "stop" => context.ShutdownAsync(caller.Token),
                "close" => context.CloseAsync(caller.Token),
                "dispose" => context.DisposeAsync().AsTask(),
                _ => throw new InvalidOperationException("Unknown test lifecycle operation.")
            };
            await Task.WhenAny(probe.Entered.Task, pending).WaitAsync(Timeout, TestToken);
            if (!probe.Entered.Task.IsCompleted)
                await pending.WaitAsync(Timeout, TestToken);
            Assert.True(probe.Entered.Task.IsCompletedSuccessfully);
            Assert.False(pending.IsCompleted);
            Assert.False(probe.ActualReturnedTask!.IsCompleted);
            string expectedOperation = operation == "dispose" ? "close" : operation;
            CancellationToken expectedToken = operation == "dispose" ? CancellationToken.None : caller.Token;
            Assert.Equal([(expectedOperation, expectedToken)], probe.Calls.ToArray());
            Assert.Empty(logger.Records);

            if (fault)
                probe.Release.TrySetException(failure);
            else
                probe.Release.TrySetResult();
            if (fault && operation == "start")
            {
                ExpectedLifecycleException observed = await Assert.ThrowsAsync<ExpectedLifecycleException>(() =>
                    pending.WaitAsync(Timeout, TestToken));
                Assert.Same(failure, observed);
                Assert.True(pending.IsFaulted);
            }
            else
            {
                await pending.WaitAsync(Timeout, TestToken);
                Assert.True(pending.IsCompletedSuccessfully);
            }
            Assert.Equal([(expectedOperation, expectedToken)], probe.Calls.ToArray());
            Assert.Single(connectionRecorder.Calls);
            if (!fault || operation == "start")
                Assert.Empty(logger.Records);
            else
            {
                LogRecord record = Assert.Single(logger.Records);
                Assert.Equal(LogLevel.Warning, record.Level);
                Assert.Same(failure, record.Exception);
                Assert.Equal(operation == "stop" ? "Stop processing client faulted: {InputAddress}" : "Close client faulted: {InputAddress}",
                    record.Values["{OriginalFormat}"]);
                Assert.Equal(input, record.Values["InputAddress"]);
            }
        }
        finally
        {
            probe.Release.TrySetResult();
            try
            {
                try
                {
                    await ObserveOwnedOutcomeAsync(probe.ActualReturnedTask, failure);
                }
                finally
                {
                    await ObserveOwnedOutcomeAsync(pending, failure);
                }
            }
            finally
            {
                LogContext.Current = previous;
                // Protected SDK mock constructors acquired no network connection; prevent duplicate recorded cleanup calls.
                probe.Closed = true;
                await context.DisposeAsync();
            }
        }
    }

    private static async Task ObserveOwnedOutcomeAsync(Task? task, Exception expected)
    {
        if (task is null)
            return;
        try
        {
            await task.WaitAsync(Timeout, CancellationToken.None);
        }
        catch (Exception exception) when (task.IsFaulted && ReferenceEquals(exception, expected))
        {
        }
        Assert.True(task.IsCompleted);
    }

    private sealed class ExpectedLifecycleException : Exception;
    private sealed record FactoryCall(string Method, object? Settings);

    private class RecordingConnection : DispatchProxy
    {
        public object Settings { get; set; } = null!;
        public ServiceBusProcessor Processor { get; set; } = null!;
        public ServiceBusSessionProcessor SessionProcessor { get; set; } = null!;
        public List<FactoryCall> Calls { get; } = [];
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            MethodInfo method = targetMethod ?? throw new InvalidOperationException("Missing proxy method.");
            Calls.Add(new FactoryCall(method.Name, args?.SingleOrDefault()));
            return method.Name switch
            {
                nameof(ConnectionContext.CreateQueueProcessor) => Processor,
                nameof(ConnectionContext.CreateQueueSessionProcessor) => SessionProcessor,
                _ => throw new NotSupportedException(method.Name)
            };
        }
    }

    private class RejectCalls : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    private sealed class ProcessorProbe
    {
        public bool Closed { get; set; }
        public ConcurrentQueue<(string Operation, CancellationToken Token)> Calls { get; } = new();
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task? ActualReturnedTask { get; private set; }
        public Task InvokeAsync(string operation, CancellationToken token)
        {
            Calls.Enqueue((operation, token));
            ActualReturnedTask = Release.Task;
            Entered.TrySetResult();
            return ActualReturnedTask;
        }
    }

    private sealed class RecordingProcessor(ProcessorProbe probe) : ServiceBusProcessor
    {
        public override string EntityPath => "queue-lifecycle";
        public override bool IsClosed => probe.Closed;
        public override Task StartProcessingAsync(CancellationToken cancellationToken = default) => probe.InvokeAsync("start", cancellationToken);
        public override Task StopProcessingAsync(CancellationToken cancellationToken = default) => probe.InvokeAsync("stop", cancellationToken);
        public override Task CloseAsync(CancellationToken cancellationToken = default) => probe.InvokeAsync("close", cancellationToken);
    }

    private sealed class RecordingSessionProcessor : ServiceBusSessionProcessor
    {
        private readonly ProcessorProbe _probe;
        private readonly RecordingProcessor _inner;
        public RecordingSessionProcessor(ProcessorProbe probe)
        {
            _probe = probe;
            _inner = new RecordingProcessor(probe);
        }
        // SDK7.20.2 exposes this mock hook; the real event accessors require a non-null InnerProcessor.
        protected override ServiceBusProcessor InnerProcessor => _inner;
        public override string EntityPath => "queue-lifecycle";
        public override bool IsClosed => _probe.Closed;
        public override Task StartProcessingAsync(CancellationToken cancellationToken = default) => _probe.InvokeAsync("start", cancellationToken);
        public override Task StopProcessingAsync(CancellationToken cancellationToken = default) => _probe.InvokeAsync("stop", cancellationToken);
        public override Task CloseAsync(CancellationToken cancellationToken = default) => _probe.InvokeAsync("close", cancellationToken);
    }

    private sealed record LogRecord(LogLevel Level, Exception? Exception, Dictionary<string, object?> Values);
    private sealed class RecordingLogger : ILogger
    {
        public ConcurrentQueue<LogRecord> Records { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Records.Enqueue(new LogRecord(logLevel, exception, ((IEnumerable<KeyValuePair<string, object?>>)state!)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)));
    }
}
