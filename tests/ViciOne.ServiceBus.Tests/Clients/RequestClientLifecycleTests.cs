using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Clients.Requests;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Clients;

public sealed class RequestClientLifecycleTests
{
    private static readonly DateTimeOffset StartTime =
        new(2032, 4, 5, 6, 7, 8, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "transport-ttl-independent-from-client-deadline")]
    public async Task DisabledTransportTimeToLive_DoesNotDisableTheClientDeadlineAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var context = new RecordingClientFactoryContext(timeProvider, new RequestTimeout(TimeSpan.FromMinutes(1)));
        var request = new LifecycleRequest("no-transport-ttl");
        using var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            SendRequestAsync,
            timeout: new RequestTimeout(TimeSpan.FromMinutes(1)),
            requestId: Guid.Parse("91d9c54e-7818-4327-b24f-aa55ee18871d"));
        handle.TimeToLive = RequestTimeout.None;

        Task<Response<LifecycleResponse>> response = handle.GetResponseAsync<LifecycleResponse>(true, TestContext.Current.CancellationToken);

        Assert.Same(request, await handle.Message);
        Assert.NotNull(context.SentContext);
        Assert.Null(context.SentContext.TimeToLive);
        await timeProvider.WaitForTimerCountAsync(1);

        timeProvider.Advance(TimeSpan.FromMinutes(1));

        RequestTimeoutException exception =
            await Assert.ThrowsAsync<RequestTimeoutException>(() => response);
        Assert.Equal(
            "Timeout waiting for response, RequestId: 91d9c54e-7818-4327-b24f-aa55ee18871d",
            exception.Message);
        return;

        async Task<LifecycleRequest> SendRequestAsync(
            Guid _,
            IPipe<SendContext<LifecycleRequest>> pipe,
            CancellationToken cancellationToken)
        {
            var sendContext = new MessageSendContext<LifecycleRequest>(request, cancellationToken);
            await pipe.SendAsync(sendContext);
            context.SentContext = sendContext;
            return request;
        }
    }

    [Theory]
    [InlineData(TerminalOutcome.CallerCancellation)]
    [InlineData(TerminalOutcome.ClientDeadline)]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "deterministic-cancellation-deadline-race")]
    public async Task CancellationAndDeadline_FirstTerminalOutcomeWinsExactlyAsync(TerminalOutcome firstOutcome)
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var context = new RecordingClientFactoryContext(timeProvider, new RequestTimeout(TimeSpan.FromMinutes(1)));
        using var callerCancellation = new CancellationTokenSource();
        var request = new LifecycleRequest(firstOutcome.ToString());
        using var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            async (_, pipe, cancellationToken) =>
            {
                var sendContext = new MessageSendContext<LifecycleRequest>(request, cancellationToken);
                await pipe.SendAsync(sendContext);
                return request;
            },
            callerCancellation.Token,
            new RequestTimeout(TimeSpan.FromMinutes(1)),
            Guid.Parse("2f79d87e-580b-4a1d-83c3-a4fb23ab393e"));
        Task<Response<LifecycleResponse>> response = handle.GetResponseAsync<LifecycleResponse>(true, TestContext.Current.CancellationToken);
        await timeProvider.WaitForTimerCountAsync(1);
        Assert.Same(request, await handle.Message);

        if (firstOutcome == TerminalOutcome.CallerCancellation)
        {
            callerCancellation.Cancel();
            TaskCanceledException exception = await Assert.ThrowsAsync<TaskCanceledException>(() => response);
            Assert.Equal(callerCancellation.Token, exception.CancellationToken);

            timeProvider.Advance(TimeSpan.FromMinutes(1));
            TaskCanceledException repeated = await Assert.ThrowsAsync<TaskCanceledException>(() => response);
            Assert.Equal(callerCancellation.Token, repeated.CancellationToken);
        }
        else
        {
            timeProvider.Advance(TimeSpan.FromMinutes(1));
            RequestTimeoutException exception =
                await Assert.ThrowsAsync<RequestTimeoutException>(() => response);
            Assert.Equal(
                "Timeout waiting for response, RequestId: 2f79d87e-580b-4a1d-83c3-a4fb23ab393e",
                exception.Message);

            callerCancellation.Cancel();
            RequestTimeoutException repeated =
                await Assert.ThrowsAsync<RequestTimeoutException>(() => response);
            Assert.Equal(exception.Message, repeated.Message);
        }

    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "one-handler-per-response-type")]
    public async Task DuplicateResponseType_IsRejectedWithoutReplacingTheOriginalHandlerAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var context = new RecordingClientFactoryContext(timeProvider, new RequestTimeout(TimeSpan.FromMinutes(1)));
        var request = new LifecycleRequest("duplicate-handler");
        var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            (_, _, _) => Task.FromResult(request),
            timeout: new RequestTimeout(TimeSpan.FromMinutes(1)));
        Task<Response<LifecycleResponse>> original = handle.GetResponseAsync<LifecycleResponse>(false, TestContext.Current.CancellationToken);

        RequestException exception = Assert.Throws<RequestException>(() =>
        {
            _ = handle.GetResponseAsync<LifecycleResponse>(false, TestContext.Current.CancellationToken);
        });

        Assert.Equal(
            $"Only one handler of type {TypeCache<LifecycleResponse>.ShortName} can be registered",
            exception.Message);
        Assert.False(original.IsCompleted);

        handle.Dispose();
        await Assert.ThrowsAsync<TaskCanceledException>(() => original);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "pre-canceled-handle-never-starts-send")]
    public async Task PreCanceledHandle_PreservesTheCallerTokenWithoutStartingTheSendAsync()
    {
        var context = new RecordingClientFactoryContext(TimeProvider.System, new RequestTimeout(TimeSpan.FromMinutes(1)));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var sendCount = 0;
        using var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            (_, _, _) =>
            {
                Interlocked.Increment(ref sendCount);
                return Task.FromResult(new LifecycleRequest("unexpected"));
            },
            cancellation.Token,
            new RequestTimeout(TimeSpan.FromMinutes(1)));

        TaskCanceledException responseCancellation = await Assert.ThrowsAsync<TaskCanceledException>(
            () => handle.GetResponseAsync<LifecycleResponse>(true, CancellationToken.None));
        TaskCanceledException messageCancellation = await Assert.ThrowsAsync<TaskCanceledException>(
            () => handle.Message);

        Assert.Equal(cancellation.Token, responseCancellation.CancellationToken);
        Assert.Equal(cancellation.Token, messageCancellation.CancellationToken);
        Assert.Equal(0, sendCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "response-wait-token-remains-effective")]
    public async Task ResponseWaitCancellation_PreservesItsOwnTokenAfterTheRequestWasSentAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var context = new RecordingClientFactoryContext(timeProvider, new RequestTimeout(TimeSpan.FromMinutes(1)));
        var request = new LifecycleRequest("separate-response-wait");
        using var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            async (_, pipe, cancellationToken) =>
            {
                await pipe.SendAsync(new MessageSendContext<LifecycleRequest>(request, cancellationToken));
                return request;
            },
            timeout: new RequestTimeout(TimeSpan.FromMinutes(1)));
        using var responseCancellation = new CancellationTokenSource();
        Task<Response<LifecycleResponse>> response = handle.GetResponseAsync<LifecycleResponse>(true, responseCancellation.Token);
        await timeProvider.WaitForTimerCountAsync(1);

        responseCancellation.Cancel();

        TaskCanceledException actual = await Assert.ThrowsAsync<TaskCanceledException>(() =>
            response.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Equal(responseCancellation.Token, actual.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "cancel-during-send-cannot-leak-late-timer")]
    public async Task CancellationDuringTheSendPipeline_DisposesATimeoutTimerCreatedAfterCancellationAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var context = new RecordingClientFactoryContext(timeProvider, new RequestTimeout(TimeSpan.FromMinutes(1)));
        var request = new LifecycleRequest("late-timer");
        using var cancellation = new CancellationTokenSource();
        var pipeEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releasePipe = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            async (_, pipe, cancellationToken) =>
            {
                await pipe.SendAsync(new MessageSendContext<LifecycleRequest>(request, cancellationToken));
                return request;
            },
            cancellation.Token,
            new RequestTimeout(TimeSpan.FromMinutes(1)));
        handle.UseExecuteAwaited(async _ =>
        {
            pipeEntered.TrySetResult();
            await releasePipe.Task;
        });
        Task<Response<LifecycleResponse>> response = handle.GetResponseAsync<LifecycleResponse>(true, CancellationToken.None);
        await pipeEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        cancellation.Cancel();
        try
        {
            TaskCanceledException actual = await Assert.ThrowsAsync<TaskCanceledException>(() =>
                response.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.Equal(cancellation.Token, actual.CancellationToken);
        }
        finally
        {
            releasePipe.TrySetResult();
        }
        await timeProvider.WaitForTimerCountAsync(1);

        Assert.Equal(1, timeProvider.TimerCount);
        Assert.Equal(0, timeProvider.ActiveTimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "cleanup-and-diagnostic-failures-do-not-stop-cancel")]
    public async Task Cancellation_WhenTimerHandlerAndDiagnosticCleanupFail_StillDisconnectsEveryHandlerAsync()
    {
        var timeProvider = new FaultingTimerTimeProvider();
        var context = new CleanupFailureClientFactoryContext(timeProvider);
        var logger = new ThrowingLogger();
        var previousLogContext = LogContext.Current;
        LogContext.ConfigureCurrentLogContext(logger);
        using var cancellation = new CancellationTokenSource();
        var request = new LifecycleRequest("cleanup-failures");

        try
        {
            using var handle = new ClientRequestHandle<LifecycleRequest>(
                context,
                async (_, pipe, cancellationToken) =>
                {
                    await pipe.SendAsync(new MessageSendContext<LifecycleRequest>(request, cancellationToken));
                    return request;
                },
                cancellation.Token,
                new RequestTimeout(TimeSpan.FromMinutes(1)));
            Task<Response<LifecycleResponse>> response = handle.GetResponseAsync<LifecycleResponse>(true, CancellationToken.None);
            await timeProvider.TimerCreated.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            cancellation.Cancel();

            TaskCanceledException actual = await Assert.ThrowsAsync<TaskCanceledException>(() =>
                response.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            await context.AllDisconnectsAttempted.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            Assert.Equal(cancellation.Token, actual.CancellationToken);
            Assert.Equal(1, timeProvider.Timer.DisposeCount);
            Assert.Equal(2, context.DisconnectCount);
            Assert.Equal(2, logger.CallCount);
        }
        finally
        {
            LogContext.Current = previousLogContext;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "terminal-cleanup-releases-send-cancellation-source")]
    public async Task DisposingACompletedHandle_ReleasesItsSendCancellationSourceAsync()
    {
        var context = new RecordingClientFactoryContext(TimeProvider.System, new RequestTimeout(TimeSpan.FromMinutes(1)));
        var request = new LifecycleRequest("dispose-cancellation-source");
        var handle = new ClientRequestHandle<LifecycleRequest>(
            context,
            (_, _, _) => Task.FromResult(request),
            timeout: new RequestTimeout(TimeSpan.FromMinutes(1)));
        Task<Response<LifecycleResponse>> response = handle.GetResponseAsync<LifecycleResponse>(true, CancellationToken.None);
        Assert.Same(request, await handle.Message);

        FieldInfo? sourceField = typeof(ClientRequestHandle<LifecycleRequest>).GetField(
            "_cancellationTokenSource",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(sourceField);
        var source = Assert.IsType<CancellationTokenSource>(sourceField.GetValue(handle));

        handle.Dispose();

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            response.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        await AssertSourceIsDisposedAsync(source);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-LIFECYCLE", "terminal-cleanup-does-not-capture-synchronization-context")]
    public async Task RequestFailure_CompletesWithoutPumpingTheAmbientSynchronizationContextAsync()
    {
        var synchronizationContext = new QueuedSynchronizationContext();
        SynchronizationContext? previous = SynchronizationContext.Current;
        ClientRequestHandle<LifecycleRequest> handle;
        Task<Response<LifecycleResponse>> response;

        SynchronizationContext.SetSynchronizationContext(synchronizationContext);
        try
        {
            handle = new ClientRequestHandle<LifecycleRequest>(
                new RecordingClientFactoryContext(TimeProvider.System, new RequestTimeout(TimeSpan.FromMinutes(1))),
                (_, _, _) => Task.FromException<LifecycleRequest>(new CleanupFailureException("Request send failed.")));
            response = handle.GetResponseAsync<LifecycleResponse>(true, CancellationToken.None);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        try
        {
            Task timeout = Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Task completed = await Task.WhenAny(response, synchronizationContext.Posted, timeout);

            Assert.Same(response, completed);
            Exception? exception = await Record.ExceptionAsync(() => response);
            Assert.NotNull(exception);
            Assert.False(synchronizationContext.HasPostedCallbacks);
        }
        finally
        {
            synchronizationContext.RunPostedCallbacks();
            handle.Dispose();

            try
            {
                await response;
            }
            catch (Exception)
            {
                // The send failure is the terminal outcome under test.
            }
        }
    }

    private static async Task AssertSourceIsDisposedAsync(CancellationTokenSource source)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        while (!timeout.IsCancellationRequested)
        {
            try
            {
                _ = source.Token;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            await Task.Yield();
        }

        Assert.Fail("The request handle did not dispose its send cancellation source after terminal cleanup.");
    }

    public enum TerminalOutcome
    {
        CallerCancellation,
        ClientDeadline,
    }

    private sealed record LifecycleRequest(string Value);

    private sealed record LifecycleResponse(string Value);

    private sealed class RecordingClientFactoryContext(
        TimeProvider timeProvider,
        RequestTimeout defaultTimeout) : ClientFactoryContext
    {
        public SendContext<LifecycleRequest>? SentContext { get; set; }

        public RequestTimeout DefaultTimeout { get; } = defaultTimeout;

        public TimeProvider TimeProvider { get; } = timeProvider;

        public IMessageRouteTable MessageRoutes { get; } = new MessageRouteTable();

        public Uri ResponseAddress { get; } = new("loopback://localhost/response");

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
            where T : class => new EmptyConnectHandle();

        public ConnectHandle ConnectConsumePipe<T>(
            IPipe<ConsumeContext<T>> pipe,
            ConnectPipeOptions options)
            where T : class => new EmptyConnectHandle();

        public ConnectHandle ConnectRequestPipe<T>(
            Guid requestId,
            IPipe<ConsumeContext<T>> pipe)
            where T : class => new EmptyConnectHandle();

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default)
            where T : class => throw new NotSupportedException();

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(
            Uri destinationAddress,
            ConsumeContext? consumeContext = default)
            where T : class => throw new NotSupportedException();
    }

    private sealed class CleanupFailureClientFactoryContext(TimeProvider timeProvider) : ClientFactoryContext
    {
        private readonly TaskCompletionSource _allDisconnectsAttempted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _connectionCount;
        private int _disconnectCount;

        public Task AllDisconnectsAttempted => _allDisconnectsAttempted.Task;

        public int DisconnectCount => Volatile.Read(ref _disconnectCount);

        public RequestTimeout DefaultTimeout => new(TimeSpan.FromMinutes(1));

        public TimeProvider TimeProvider { get; } = timeProvider;

        public IMessageRouteTable MessageRoutes { get; } = new MessageRouteTable();

        public Uri ResponseAddress { get; } = new("loopback://localhost/cleanup-response");

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
            where T : class => CreateConnection();

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
            where T : class => CreateConnection();

        public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
            where T : class => CreateConnection();

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default)
            where T : class => throw new NotSupportedException();

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(Uri destinationAddress, ConsumeContext? consumeContext = default)
            where T : class => throw new NotSupportedException();

        private ConnectHandle CreateConnection()
        {
            int connection = Interlocked.Increment(ref _connectionCount);
            return new CleanupConnectHandle(this, throwOnDisconnect: connection == 2);
        }

        private void RecordDisconnect(bool throwOnDisconnect)
        {
            if (Interlocked.Increment(ref _disconnectCount) == 2)
                _allDisconnectsAttempted.TrySetResult();

            if (throwOnDisconnect)
                throw new CleanupFailureException("Response handler disconnect failed.");
        }

        private sealed class CleanupConnectHandle(CleanupFailureClientFactoryContext owner, bool throwOnDisconnect) : ConnectHandle
        {
            private int _disconnected;

            public void Disconnect()
            {
                if (Interlocked.Exchange(ref _disconnected, 1) == 0)
                    owner.RecordDisconnect(throwOnDisconnect);
            }

            public void Dispose() => Disconnect();
        }
    }

    private sealed class FaultingTimerTimeProvider : TimeProvider
    {
        private readonly TaskCompletionSource _timerCreated =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public FaultingTimer Timer { get; } = new();

        public Task TimerCreated => _timerCreated.Task;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            _timerCreated.TrySetResult();
            return Timer;
        }

        public sealed class FaultingTimer : ITimer
        {
            private int _disposeCount;

            public int DisposeCount => Volatile.Read(ref _disposeCount);

            public bool Change(TimeSpan dueTime, TimeSpan period) => true;

            public void Dispose()
            {
                Interlocked.Increment(ref _disposeCount);
                throw new CleanupFailureException("Request timer disposal failed.");
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return default;
            }
        }
    }

    private sealed class ThrowingLogger : ILogger
    {
        private int _callCount;

        public int CallCount => Volatile.Read(ref _callCount);

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Interlocked.Increment(ref _callCount);
            throw new InvalidOperationException("Diagnostic logger failure.");
        }
    }

    private sealed class QueuedSynchronizationContext : SynchronizationContext
    {
        private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _callbacks = new();
        private readonly TaskCompletionSource _posted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool HasPostedCallbacks => !_callbacks.IsEmpty;

        public Task Posted => _posted.Task;

        public override void Post(SendOrPostCallback callback, object? state)
        {
            _callbacks.Enqueue((callback, state));
            _posted.TrySetResult();
        }

        public void RunPostedCallbacks()
        {
            while (_callbacks.TryDequeue(out var callback))
                callback.Callback(callback.State);
        }
    }

    private sealed class CleanupFailureException(string message) : Exception(message);
}
