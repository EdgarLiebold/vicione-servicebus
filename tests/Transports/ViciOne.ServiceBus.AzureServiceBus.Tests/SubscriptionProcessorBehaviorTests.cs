using System.Collections.Concurrent;
using System.Reflection;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class SubscriptionProcessorBehaviorTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    public static IEnumerable<object[]> DispatchCases =>
        from session in new[] { false, true }
        from error in new[] { false, true }
        from outcome in new[] { 0, 1, 2 }
        select new object[] { session, error, outcome };

    public static IEnumerable<object[]> LifecycleCases =>
        from session in new[] { false, true }
        from operation in new[] { "start", "stop", "close", "dispose" }
        from outcome in new[] { 0, 1, 2 }
        select new object[] { session, operation, outcome };

    [Theory]
    [MemberData(nameof(DispatchCases))]
    [RequirementCoverage("REQ-VSB-ASB-SUBSCRIPTION-PROCESSOR", "sdk-callbacks-preserve-identity-and-await-terminal-result")]
    public async Task Dispatch_PreservesCallbackIdentityAndWaitsForItsResultAsync(bool session, bool error, int outcome)
    {
        await using var fixture = new Fixture(session);
        using var delivery = new CancellationTokenSource();
        using var completionToken = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new InvalidOperationException("callback failed");
        object? seenArgs = null;
        ServiceBusReceivedMessage? seenMessage = null;
        CancellationToken seenToken = default;
        int calls = 0;
        Task RecordAsync(object args, ServiceBusReceivedMessage? message, CancellationToken token)
        {
            seenArgs = args;
            seenMessage = message;
            seenToken = token;
            calls++;
            entered.TrySetResult();
            return release.Task;
        }
        fixture.Configure(session, (args, message, token) => RecordAsync(args, message, token),
            args => RecordAsync(args, null, args.CancellationToken));
        ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("subscription-payload"), messageId: "message-37", sessionId: "session-11");
        object args = error
            ? new ProcessErrorEventArgs(new InvalidOperationException("sdk fault"), ServiceBusErrorSource.Receive,
                "unit.servicebus.invalid", "orders/Subscriptions/accounting", delivery.Token)
            : fixture.MessageArgs(message, delivery.Token);
        Task? pending = null;
        try
        {
            pending = error ? fixture.RaiseErrorAsync((ProcessErrorEventArgs)args) : fixture.RaiseMessageAsync(args);
            await Task.WhenAny(entered.Task, pending).WaitAsync(Timeout, TestToken);
            if (!entered.Task.IsCompleted)
                await pending.WaitAsync(Timeout, TestToken);
            await entered.Task.WaitAsync(Timeout, TestToken);
            Assert.False(pending.IsCompleted);
            Assert.Same(args, seenArgs);
            Assert.Equal(delivery.Token, seenToken);
            if (error)
                Assert.Null(seenMessage);
            else
                Assert.Same(message, seenMessage);
            Assert.Equal(1, calls);
            fixture.AssertFactorySelection();

            await ReleaseAsync(release, outcome, failure, completionToken);
            await AssertResultAsync(pending, outcome, failure, completionToken.Token);
            Assert.Equal(1, calls);
        }
        finally
        {
            release.TrySetResult();
            await ObserveCleanupAsync(pending);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-ASB-SUBSCRIPTION-PROCESSOR", "reconfiguration-rejects-all-kind-combinations-and-retains-original-callback")]
    public async Task Reconfiguration_RejectsEveryKindCombinationAndPreservesOriginalCallbackAsync(bool firstSession, bool secondSession)
    {
        await using var fixture = new Fixture(firstSession);
        int originalCalls = 0;
        int replacementCalls = 0;
        fixture.Configure(firstSession, (_, _, _) => { originalCalls++; return Task.CompletedTask; }, _ => Task.CompletedTask);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
            fixture.Configure(secondSession, (_, _, _) => { replacementCalls++; return Task.CompletedTask; }, _ => Task.CompletedTask));

        string expected = (firstSession, secondSession) switch
        {
            (false, false) => "The message processor has already been configured.",
            (true, true) => "The session processor has already been configured.",
            (true, false) => "A message processor cannot be configured after a session processor.",
            _ => "A session processor cannot be configured after a message processor."
        };
        Assert.Equal(expected, error.Message);
        fixture.AssertFactorySelection();
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(messageId: "after-rejection");
        await fixture.RaiseMessageAsync(fixture.MessageArgs(message, TestToken)).WaitAsync(Timeout, TestToken);
        Assert.Equal(1, originalCalls);
        Assert.Equal(0, replacementCalls);
    }

    [Theory]
    [MemberData(nameof(LifecycleCases))]
    [RequirementCoverage("REQ-VSB-ASB-SUBSCRIPTION-PROCESSOR", "lifecycle-awaits-sdk-and-preserves-propagation-or-warning-contract")]
    public async Task Lifecycle_AwaitsSdkAndPreservesFailureContractAsync(bool session, string operation, int outcome)
    {
        await using var fixture = new Fixture(session);
        fixture.Configure(session, (_, _, _) => Task.CompletedTask, _ => Task.CompletedTask);
        using var caller = new CancellationTokenSource();
        using var completionToken = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new InvalidOperationException("sdk lifecycle failed");
        var logger = new RecordingLogger();
        ILogContext? previous = LogContext.Current;
        fixture.Probe.Handler = (_, _) => { entered.TrySetResult(); return release.Task; };
        Task? pending = null;
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            pending = operation switch
            {
                "start" => fixture.Context.StartAsync(caller.Token),
                "stop" => fixture.Context.ShutdownAsync(caller.Token),
                "close" => fixture.Context.CloseAsync(caller.Token),
                _ => fixture.Context.DisposeAsync().AsTask()
            };
            await entered.Task.WaitAsync(Timeout, TestToken);
            Assert.False(pending.IsCompleted);
            string expectedOperation = operation == "dispose" ? "close" : operation;
            CancellationToken expectedToken = operation == "dispose" ? CancellationToken.None : caller.Token;
            Assert.Equal([(expectedOperation, expectedToken)], fixture.Probe.Calls.ToArray());
            Assert.Empty(logger.Records);

            await ReleaseAsync(release, outcome, failure, completionToken);
            await AssertResultAsync(pending, operation == "start" ? outcome : 0, failure, completionToken.Token);

            Assert.Equal([(expectedOperation, expectedToken)], fixture.Probe.Calls.ToArray());
            if (operation == "start" || outcome == 0)
                Assert.Empty(logger.Records);
            else
            {
                LogRecord log = Assert.Single(logger.Records);
                Assert.Equal(LogLevel.Warning, log.Level);
                Assert.Equal(operation == "stop" ? "Stop processing client faulted: {InputAddress}" : "Close client faulted: {InputAddress}",
                    log.Values["{OriginalFormat}"]);
                Assert.Equal(fixture.Context.InputAddress, log.Values["InputAddress"]);
                if (outcome == 1)
                    Assert.Same(failure, log.Exception);
                else
                    Assert.Equal(completionToken.Token, Assert.IsAssignableFrom<OperationCanceledException>(log.Exception).CancellationToken);
            }
        }
        finally
        {
            release.TrySetResult();
            try
            {
                await ObserveCleanupAsync(pending);
            }
            finally
            {
                LogContext.Current = previous;
            }
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-ASB-SUBSCRIPTION-PROCESSOR", "closed-processors-skip-stop-close-and-dispose-uses-default-token")]
    public async Task ClosedState_ControlsStopCloseAndDisposeUsesDefaultTokenAsync(bool session, bool closed)
    {
        await using var fixture = new Fixture(session);
        Assert.False(fixture.Context.IsClosedOrClosing);
        fixture.Configure(session, (_, _, _) => Task.CompletedTask, _ => Task.CompletedTask);
        fixture.Probe.Closed = closed;
        Assert.Equal(closed, fixture.Context.IsClosedOrClosing);
        using var caller = new CancellationTokenSource();

        await fixture.Context.ShutdownAsync(caller.Token).WaitAsync(Timeout, TestToken);
        await fixture.Context.CloseAsync(caller.Token).WaitAsync(Timeout, TestToken);
        await fixture.Context.DisposeAsync().AsTask().WaitAsync(Timeout, TestToken);

        if (closed)
            Assert.Empty(fixture.Probe.Calls);
        else
            Assert.Equal([("stop", caller.Token), ("close", caller.Token), ("close", CancellationToken.None)], fixture.Probe.Calls.ToArray());
        fixture.AssertFactorySelection();
    }

    private static async Task ReleaseAsync(TaskCompletionSource gate, int outcome, Exception failure, CancellationTokenSource canceled)
    {
        if (outcome == 0)
            gate.SetResult();
        else if (outcome == 1)
            gate.SetException(failure);
        else
        {
            await canceled.CancelAsync();
            gate.SetCanceled(canceled.Token);
        }
    }

    private static async Task AssertResultAsync(Task pending, int outcome, Exception failure, CancellationToken token)
    {
        if (outcome == 0)
            await pending.WaitAsync(Timeout, TestToken);
        else if (outcome == 1)
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => pending.WaitAsync(Timeout, TestToken)));
        else
        {
            OperationCanceledException error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(Timeout, TestToken));
            Assert.Equal(token, error.CancellationToken);
            Assert.True(pending.IsCanceled);
        }
    }

    private static async Task ObserveCleanupAsync(Task? task)
    {
        if (task is null)
            return;
        try
        {
            await task.WaitAsync(Timeout, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
        }
        catch (OperationCanceledException)
        {
        }
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly bool _session;
        private readonly ServiceBusClient _client = new("Endpoint=sb://unit.servicebus.invalid/;SharedAccessKeyName=unit;SharedAccessKey=dGVzdA==");
        private readonly RecordingProcessor _messageProcessor;
        private readonly RecordingSessionProcessor _sessionProcessor;
        private readonly SubscriptionSettings _settings;
        private readonly List<(string Method, object? Settings)> _factories = [];

        public Fixture(bool session)
        {
            _session = session;
            _messageProcessor = new RecordingProcessor(Probe);
            _sessionProcessor = new RecordingSessionProcessor(Probe, _client);
            _settings = DispatchProxy.Create<SubscriptionSettings, Stub>();
            ((Stub)_settings).Handler = (method, _) => method.Name == "get_CreateTopicOptions"
                ? new CreateTopicOptions("orders") : throw new NotSupportedException(method.Name);
            ConnectionContext connection = DispatchProxy.Create<ConnectionContext, Stub>();
            ((Stub)connection).Handler = (method, args) =>
            {
                _factories.Add((method.Name, args.SingleOrDefault()));
                return method.Name switch
                {
                    nameof(ConnectionContext.CreateSubscriptionProcessor) => _messageProcessor,
                    nameof(ConnectionContext.CreateSubscriptionSessionProcessor) => _sessionProcessor,
                    _ => throw new NotSupportedException(method.Name)
                };
            };
            Context = new SubscriptionClientContext(connection, new Uri("sb://unit.servicebus.invalid/orders/Subscriptions/accounting"), _settings, null!);
        }

        public ProcessorProbe Probe { get; } = new();
        public SubscriptionClientContext Context { get; }

        public void Configure(bool session, Func<object, ServiceBusReceivedMessage, CancellationToken, Task> message,
            Func<ProcessErrorEventArgs, Task> error)
        {
            if (session)
                Context.ConfigureSessionProcessor((args, payload, token) => message(args, payload, token), error);
            else
                Context.ConfigureMessageProcessor((args, payload, token) => message(args, payload, token), error);
        }

        public object MessageArgs(ServiceBusReceivedMessage message, CancellationToken token) => _session
            ? new ProcessSessionMessageEventArgs(message, new SessionReceiver(), token)
            : new ProcessMessageEventArgs(message, new MessageReceiver(), token);

        public Task RaiseMessageAsync(object args) => _session
            ? _sessionProcessor.RaiseMessageAsync((ProcessSessionMessageEventArgs)args)
            : _messageProcessor.RaiseMessageAsync((ProcessMessageEventArgs)args);

        public Task RaiseErrorAsync(ProcessErrorEventArgs args) => _session
            ? _sessionProcessor.RaiseErrorAsync(args) : _messageProcessor.RaiseErrorAsync(args);

        public void AssertFactorySelection()
        {
            var factory = Assert.Single(_factories);
            Assert.Equal(_session ? nameof(ConnectionContext.CreateSubscriptionSessionProcessor) : nameof(ConnectionContext.CreateSubscriptionProcessor), factory.Method);
            Assert.Same(_settings, factory.Settings);
            Assert.Equal("orders", Context.EntityPath);
            Assert.Equal(new Uri("sb://unit.servicebus.invalid/orders/Subscriptions/accounting"), Context.InputAddress);
        }

        public async ValueTask DisposeAsync()
        {
            Probe.Closed = true;
            Probe.Handler = (_, _) => Task.CompletedTask;
            await Context.DisposeAsync();
            await _client.DisposeAsync();
        }
    }

    private sealed class ProcessorProbe
    {
        public bool Closed { get; set; }
        public ConcurrentQueue<(string Operation, CancellationToken Token)> Calls { get; } = new();
        public Func<string, CancellationToken, Task> Handler { get; set; } = (_, _) => Task.CompletedTask;
        public Task InvokeAsync(string operation, CancellationToken token)
        {
            Calls.Enqueue((operation, token));
            return Handler(operation, token);
        }
    }

    private sealed class RecordingProcessor(ProcessorProbe probe) : ServiceBusProcessor
    {
        public override bool IsClosed => probe.Closed;
        public override Task StartProcessingAsync(CancellationToken cancellationToken = default) => probe.InvokeAsync("start", cancellationToken);
        public override Task StopProcessingAsync(CancellationToken cancellationToken = default) => probe.InvokeAsync("stop", cancellationToken);
        public override Task CloseAsync(CancellationToken cancellationToken = default) => probe.InvokeAsync("close", cancellationToken);
        public Task RaiseMessageAsync(ProcessMessageEventArgs args) => OnProcessMessageAsync(args);
        public Task RaiseErrorAsync(ProcessErrorEventArgs args) => OnProcessErrorAsync(args);
    }

    private sealed class RecordingSessionProcessor(ProcessorProbe probe, ServiceBusClient client)
        : ServiceBusSessionProcessor(client, "orders", "accounting", new ServiceBusSessionProcessorOptions())
    {
        public override bool IsClosed => probe.Closed;
        public override Task StartProcessingAsync(CancellationToken cancellationToken = default) => probe.InvokeAsync("start", cancellationToken);
        public override Task StopProcessingAsync(CancellationToken cancellationToken = default) => probe.InvokeAsync("stop", cancellationToken);
        public override Task CloseAsync(CancellationToken cancellationToken = default) => probe.InvokeAsync("close", cancellationToken);
        public Task RaiseMessageAsync(ProcessSessionMessageEventArgs args) => OnProcessSessionMessageAsync(args);
        public Task RaiseErrorAsync(ProcessErrorEventArgs args) => OnProcessErrorAsync(args);
    }

    private sealed class MessageReceiver : ServiceBusReceiver;
    private sealed class SessionReceiver : ServiceBusSessionReceiver;

    private class Stub : DispatchProxy
    {
        public Func<MethodInfo, object?[], object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!, args ?? []);
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
