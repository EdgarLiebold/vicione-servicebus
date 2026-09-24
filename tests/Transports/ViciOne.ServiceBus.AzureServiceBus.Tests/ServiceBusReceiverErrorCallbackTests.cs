using System.Reflection;
using System.Net.WebSockets;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusReceiverErrorCallbackTests
{
    [Theory]
    [InlineData(ServiceBusFailureReason.ServiceCommunicationProblem, true, true)]
    [InlineData(ServiceBusFailureReason.ServiceCommunicationProblem, false, true)]
    [InlineData(ServiceBusFailureReason.MessagingEntityNotFound, false, true)]
    [InlineData(ServiceBusFailureReason.MessageLockLost, false, false)]
    [InlineData(ServiceBusFailureReason.SessionLockLost, false, false)]
    [InlineData(ServiceBusFailureReason.MessagingEntityDisabled, false, false)]
    [InlineData(ServiceBusFailureReason.ServiceTimeout, true, false)]
    [InlineData(ServiceBusFailureReason.ServiceTimeout, false, true)]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "queue-error-recycle-reasons-and-fault-identity")]
    public async Task QueueProcessorErrorCallback_RecyclesOnlyTheRequiredServiceBusFailuresAsync(
        ServiceBusFailureReason reason, bool isTransient, bool expectedRecycle)
    {
        (Receiver receiver, RecordingClientContext client) = CreateReceiver(session: false);
        receiver.Start();
        await receiver.Ready;

        Func<ProcessErrorEventArgs, Task> callback =
            Assert.IsType<Func<ProcessErrorEventArgs, Task>>(client.MessageErrorHandler);
        Assert.Null(client.SessionErrorHandler);
        var failure = new ServiceBusException(isTransient, "processor failure", "exception-entity", reason, null);
        string sdkEntityPath = $"affected/{reason}";

        await callback(CreateError(failure, sdkEntityPath));

        if (expectedRecycle)
        {
            (Exception exception, string entityPath) = Assert.Single(client.FaultNotifications);
            Assert.Same(failure, exception);
            Assert.Equal(sdkEntityPath, entityPath);
        }
        else
        {
            Assert.Empty(client.FaultNotifications);
            Assert.False(receiver.Stopping.IsCancellationRequested);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "unknown-nontransient-error-recycles")]
    public async Task QueueProcessorErrorCallback_UnknownNonTransientFailureRequestsRecycleAsync()
    {
        (Receiver receiver, RecordingClientContext client) = CreateReceiver(session: false);
        receiver.Start();
        await receiver.Ready;
        var failure = new InvalidOperationException("unclassified processor failure");

        await Assert.IsType<Func<ProcessErrorEventArgs, Task>>(client.MessageErrorHandler)(
            CreateError(failure, "unknown/affected-entity"));

        (Exception exception, string entityPath) = Assert.Single(client.FaultNotifications);
        Assert.Same(failure, exception);
        Assert.Equal("unknown/affected-entity", entityPath);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "expired-message-and-lock-do-not-recycle")]
    public async Task QueueProcessorErrorCallback_ExpiredMessageAndLockDoNotRecycleAsync()
    {
        (Receiver receiver, RecordingClientContext client) = CreateReceiver(session: false);
        receiver.Start();
        await receiver.Ready;
        Func<ProcessErrorEventArgs, Task> callback =
            Assert.IsType<Func<ProcessErrorEventArgs, Task>>(client.MessageErrorHandler);

        foreach (Exception failure in new Exception[]
        {
            new MessageTimeToLiveExpiredException(),
            new MessageLockExpiredException(),
        })
        {
            await callback(CreateError(failure, "expired/affected-entity"));
            Assert.Empty(client.FaultNotifications);
            Assert.False(receiver.Stopping.IsCancellationRequested);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "session-error-uses-inherited-recycle-handler")]
    public async Task SessionProcessorErrorCallback_UsesTheInheritedRecycleHandlerAsync()
    {
        (Receiver receiver, RecordingClientContext client) = CreateReceiver(session: true);
        receiver.Start();
        await receiver.Ready;
        var failure = new ServiceBusException(true, "connection unavailable", "exception-entity",
            ServiceBusFailureReason.ServiceCommunicationProblem, null);

        Assert.Null(client.MessageErrorHandler);
        await Assert.IsType<Func<ProcessErrorEventArgs, Task>>(client.SessionErrorHandler)(
            CreateError(failure, "session/affected-entity"));

        (Exception exception, string entityPath) = Assert.Single(client.FaultNotifications);
        Assert.Same(failure, exception);
        Assert.Equal("session/affected-entity", entityPath);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "error-callback-awaits-fault-notification")]
    public async Task QueueProcessorErrorCallback_WaitsForFaultNotificationBeforeCompletingAsync()
    {
        (Receiver receiver, RecordingClientContext client) = CreateReceiver(session: false);
        var notificationCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.FaultNotificationCompletion = notificationCompletion.Task;
        receiver.Start();
        await receiver.Ready;
        var failure = new InvalidOperationException("unclassified processor failure");

        Task callback = Assert.IsType<Func<ProcessErrorEventArgs, Task>>(client.MessageErrorHandler)(
            CreateError(failure, "pending/affected-entity"));

        try
        {
            (Exception exception, string entityPath) = Assert.Single(client.FaultNotifications);
            Assert.Same(failure, exception);
            Assert.Equal("pending/affected-entity", entityPath);
            Assert.False(callback.IsCompleted);
            Assert.False(receiver.Stopping.IsCancellationRequested);
            Assert.False(receiver.Completed.IsCompleted);
        }
        finally
        {
            notificationCompletion.SetResult(true);
        }

        await callback;
        await receiver.Completed.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(receiver.Stopping.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "processor-errors-preserve-severity-structured-identity-and-quiet-cases")]
    public async Task QueueProcessorErrorCallback_LogsTheClassifiedFailureWithExactStructuredIdentityAsync()
    {
        var cases = new (Exception Failure, LogLevel? Level, string? Template, bool Recycle)[]
        {
            (new ServiceBusException(true, "connection", "entity", ServiceBusFailureReason.ServiceCommunicationProblem,
                new TimeoutException()), LogLevel.Debug,
                "ServiceBusException on Receiver {InputAddress} during {Action} ActiveDispatchCount({activeDispatch}) ErrorRequiresRecycle({requiresRecycle})", true),
            (new ServiceBusException(false, "connection", "entity", ServiceBusFailureReason.ServiceCommunicationProblem, null), LogLevel.Error,
                "Exception on Receiver {InputAddress} during {Action} ActiveDispatchCount({activeDispatch}) ErrorRequiresRecycle({requiresRecycle})", true),
            (new WebSocketException(WebSocketError.ConnectionClosedPrematurely, "socket", new TimeoutException()), LogLevel.Debug,
                "WebSocketException on Receiver {InputAddress} code {Code} ActiveDispatchCount({activeDispatch}) ErrorRequiresRecycle({requiresRecycle})", true),
            (new ServiceBusException(true, "timeout", "entity", ServiceBusFailureReason.ServiceTimeout, null), LogLevel.Warning,
                "Exception on Receiver {InputAddress} during {Action} ActiveDispatchCount({activeDispatch}) ErrorRequiresRecycle({requiresRecycle})", false),
            (new InvalidOperationException("unknown"), LogLevel.Error,
                "Exception on Receiver {InputAddress} during {Action} ActiveDispatchCount({activeDispatch}) ErrorRequiresRecycle({requiresRecycle})", true),
            (new ObjectDisposedException("$cbs"), null, null, true),
            (new ObjectDisposedException("unrelated"), LogLevel.Error,
                "Exception on Receiver {InputAddress} during {Action} ActiveDispatchCount({activeDispatch}) ErrorRequiresRecycle({requiresRecycle})", true),
            (new ServiceBusException(false, "lock", "entity", ServiceBusFailureReason.MessageLockLost, null), null, null, false),
            (new ServiceBusException(false, "session", "entity", ServiceBusFailureReason.SessionLockLost, null), null, null, false),
            (new ServiceBusException(false, "disabled", "entity", ServiceBusFailureReason.MessagingEntityDisabled, null), null, null, false),
            (new OperationCanceledException(), null, null, true),
            (new InvalidOperationException("wrapped timeout", new TimeoutException()), null, null, true),
        };

        ILogContext? previous = LogContext.Current;
        try
        {
            foreach ((Exception failure, LogLevel? level, string? template, bool recycle) in cases)
            {
                var logger = new RecordingLogger();
                LogContext.ConfigureCurrentLogContext(logger);
                (Receiver receiver, RecordingClientContext client) = CreateReceiver(session: false);
                receiver.Start();
                await receiver.Ready;

                await Assert.IsType<Func<ProcessErrorEventArgs, Task>>(client.MessageErrorHandler)(
                    CreateError(failure, "logged/affected-entity"));

                Assert.Equal(recycle ? 1 : 0, client.FaultNotifications.Count);
                if (recycle)
                    Assert.Same(failure, client.FaultNotifications[0].Exception);

                LogEntry[] receiverEntries = logger.Entries
                    .Where(entry => entry.Template.Contains("on Receiver", StringComparison.Ordinal))
                    .ToArray();
                if (level is null)
                {
                    Assert.Empty(receiverEntries);
                    continue;
                }

                LogEntry entry = Assert.Single(receiverEntries);
                Assert.Equal(level, entry.Level);
                Assert.Same(failure, entry.Exception);
                Assert.Equal(template, entry.Template);
                Assert.Equal(new Uri("sb://unit.servicebus.invalid/configured-input"), entry.Values["InputAddress"]);
                Assert.Equal(0L, Assert.IsType<long>(entry.Values["activeDispatch"]));
                Assert.Equal(recycle, entry.Values["requiresRecycle"]);
                if (failure is WebSocketException socket)
                {
                    Assert.Equal(socket.WebSocketErrorCode, entry.Values["Code"]);
                    Assert.Equal(5, entry.Values.Count);
                }
                else
                {
                    Assert.Equal(ServiceBusErrorSource.Receive, entry.Values["Action"]);
                    Assert.Equal(5, entry.Values.Count);
                }
            }
        }
        finally
        {
            LogContext.Current = previous;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "recycle-faults-receiver-lifecycle")]
    public async Task QueueProcessorErrorCallback_RecycleStopsReceiverButIgnoredFailureLeavesItRunningAsync()
    {
        (Receiver receiver, RecordingClientContext client) = CreateReceiver(session: false);
        receiver.Start();
        await receiver.Ready;
        Func<ProcessErrorEventArgs, Task> callback =
            Assert.IsType<Func<ProcessErrorEventArgs, Task>>(client.MessageErrorHandler);

        var ignoredFailure = new ServiceBusException(false, "message lock lost", "exception-entity",
            ServiceBusFailureReason.MessageLockLost, null);
        await callback(CreateError(ignoredFailure, "ignored/affected-entity"));

        Assert.Empty(client.FaultNotifications);
        Assert.False(receiver.Stopping.IsCancellationRequested);
        Assert.False(receiver.Completed.IsCompleted);

        var recycleFailure = new ServiceBusException(true, "connection unavailable", "exception-entity",
            ServiceBusFailureReason.ServiceCommunicationProblem, null);
        await callback(CreateError(recycleFailure, "recycled/affected-entity"));

        (Exception exception, string entityPath) = Assert.Single(client.FaultNotifications);
        Assert.Same(recycleFailure, exception);
        Assert.Equal("recycled/affected-entity", entityPath);

        using var testCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var stopping = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = receiver.Stopping.Register(() => stopping.TrySetResult(true));
        await stopping.Task.WaitAsync(testCancellation.Token);
        await receiver.Completed.WaitAsync(testCancellation.Token);

        Assert.True(receiver.Stopping.IsCancellationRequested);
        Assert.True(receiver.Completed.IsCompletedSuccessfully);
    }

    private static ProcessErrorEventArgs CreateError(Exception exception, string entityPath) =>
        new(exception, ServiceBusErrorSource.Receive, "unit.servicebus.invalid", entityPath, CancellationToken.None);

    private static (Receiver Receiver, RecordingClientContext Client) CreateReceiver(bool session)
    {
        ClientContext client = DispatchProxy.Create<ClientContext, RecordingClientContext>();
        ServiceBusReceiveEndpointContext endpoint =
            DispatchProxy.Create<ServiceBusReceiveEndpointContext, RecordingReceiveEndpointContext>();

        return (session ? new SessionReceiver(client, endpoint) : new Receiver(client, endpoint),
            (RecordingClientContext)(object)client);
    }

    private class RecordingClientContext : DispatchProxy
    {
        public Func<ProcessErrorEventArgs, Task>? MessageErrorHandler { get; private set; }

        public Func<ProcessErrorEventArgs, Task>? SessionErrorHandler { get; private set; }

        public List<(Exception Exception, string EntityPath)> FaultNotifications { get; } = [];

        public Task? FaultNotificationCompletion { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            switch (targetMethod?.Name)
            {
                case nameof(ClientContext.ConfigureMessageProcessor):
                    MessageErrorHandler = (Func<ProcessErrorEventArgs, Task>)args![1]!;
                    return null;
                case nameof(ClientContext.ConfigureSessionProcessor):
                    SessionErrorHandler = (Func<ProcessErrorEventArgs, Task>)args![1]!;
                    return null;
                case nameof(ClientContext.StartAsync):
                case nameof(ClientContext.ShutdownAsync):
                case nameof(ClientContext.CloseAsync):
                    return Task.CompletedTask;
                case nameof(ClientContext.NotifyFaultedAsync):
                    FaultNotifications.Add(((Exception)args![0]!, (string)args[1]!));
                    return FaultNotificationCompletion ?? Task.CompletedTask;
                case "get_InputAddress":
                    return new Uri("sb://unit.servicebus.invalid/configured-input");
                default:
                    throw new NotSupportedException($"Unexpected client context member: {targetMethod?.Name}");
            }
        }
    }

    private class RecordingReceiveEndpointContext : DispatchProxy
    {
        private readonly IReceivePipeDispatcher _dispatcher =
            DispatchProxy.Create<IReceivePipeDispatcher, RecordingDispatcher>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            nameof(ServiceBusReceiveEndpointContext.CreateReceivePipeDispatcher) => _dispatcher,
            "get_LogContext" or "get_StopTimeout" or "get_ConsumerStopTimeout" => null,
            "get_InputAddress" => new Uri("sb://unit.servicebus.invalid/configured-input"),
            _ => throw new NotSupportedException($"Unexpected receive endpoint member: {targetMethod?.Name}"),
        };
    }

    private class RecordingDispatcher : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "add_ZeroActivity" or "remove_ZeroActivity" => null,
            "get_ActiveDispatchCount" => 0,
            _ => throw new NotSupportedException($"Unexpected dispatcher member: {targetMethod?.Name}"),
        };
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var values = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(state)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            Entries.Add(new LogEntry(logLevel, exception, Assert.IsType<string>(values["{OriginalFormat}"]), values));
        }
    }

    private sealed record LogEntry(LogLevel Level, Exception? Exception, string Template,
        Dictionary<string, object?> Values);
}
