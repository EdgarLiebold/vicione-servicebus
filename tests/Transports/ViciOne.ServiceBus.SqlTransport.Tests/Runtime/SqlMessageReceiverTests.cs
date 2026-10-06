using System.Reflection;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.SqlTransport.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Runtime;

public sealed class SqlMessageReceiverTests
{
    private static readonly DateTimeOffset Now = new(2038, 5, 6, 7, 8, 9, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-RECEIVER-LIFECYCLE", "empty-poll-maintains-wakes-and-stops")]
    public async Task EmptyPoll_MaintainsQueueWakesEarlyAndStopsThePendingRequestAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness = new ReceiverHarness([]);

        await harness.Receiver.Ready.WaitAsync(cancellationToken);
        await harness.Connection.DelayEntered.WaitAsync(cancellationToken);

        Assert.Equal((1, 1, 1), (harness.Client.ReceiveCallCount, harness.Client.DeadLetterCallCount, harness.Client.TouchCallCount));

        harness.Receiver.MessageHandled();
        await harness.Client.SecondReceiveEntered.WaitAsync(cancellationToken);
        await harness.StopAsync(cancellationToken);

        Assert.Equal(2, harness.Client.ReceiveCallCount);
        Assert.True(harness.Client.LastReceiveCancellationToken.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-RECEIVER-LIFECYCLE", "completion-before-wait-is-not-lost")]
    public async Task MessageHandledDuringMaintenance_SkipsTheStalePollingWaitAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness = new ReceiverHarness([], holdFirstReceive: true);

        await harness.Client.FirstReceiveEntered.WaitAsync(cancellationToken);
        harness.Client.OnDeadLetter = harness.Receiver.MessageHandled;
        harness.Client.ReleaseFirstReceive();

        await harness.Client.SecondReceiveEntered.WaitAsync(cancellationToken);
        await harness.StopAsync(cancellationToken);

        Assert.Equal(0, harness.Connection.DelayCallCount);
        Assert.Equal(2, harness.Client.ReceiveCallCount);
    }

    [Theory]
    [InlineData(5, 2.5)]
    [InlineData(0.5, 0.5)]
    [RequirementCoverage("REQ-VSB-SQL-RECEIVER-LIFECYCLE", "auto-delete-polling-is-bounded-by-keepalive")]
    public async Task AutoDeleteQueue_PollsSoonEnoughToKeepTheActiveQueueAliveAsync(double autoDeleteSeconds, double expectedPollingSeconds)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness = new ReceiverHarness([], autoDeleteOnIdle: TimeSpan.FromSeconds(autoDeleteSeconds),
            pollingInterval: TimeSpan.FromMinutes(1));

        await harness.Connection.DelayEntered.WaitAsync(cancellationToken);
        await harness.StopAsync(cancellationToken);

        Assert.Equal(TimeSpan.FromSeconds(expectedPollingSeconds), harness.Connection.LastPollingInterval);
        Assert.Equal(1, harness.Client.TouchCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-RECEIVER-LIFECYCLE", "active-message-dispatches-and-settles")]
    public async Task ActiveMessage_IsDispatchedWithItsTransportIdentityAndSettledAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SqlTransportMessage message = Message();
        await using var harness = new ReceiverHarness([message]);

        await harness.Dispatcher.DispatchCompleted.WaitAsync(cancellationToken);
        await harness.Client.SecondReceiveEntered.WaitAsync(cancellationToken);
        await harness.StopAsync(cancellationToken);

        SqlReceiveContext context = Assert.IsType<SqlReceiveContext>(harness.Dispatcher.LastContext);
        Assert.Equal(message.TransportMessageId, context.TransportMessageId);
        Assert.Equal(message.QueueName, context.QueueName);
        Assert.Equal((1, 1), (harness.Dispatcher.DispatchCount, harness.Client.DeleteCallCount));
        Assert.Equal((0, 0), (harness.Client.MoveCallCount, harness.Client.UnlockCallCount));
    }

    [Theory]
    [InlineData(1, false, null)]
    [InlineData(2, true, 1)]
    [RequirementCoverage("REQ-VSB-SQL-FIRST-DELIVERY", "fetched-attempt-count-agrees-with-redelivery-metadata")]
    public async Task FetchedDelivery_AttemptCountAgreesWithRedeliveredAndHeaderAsync(
        int deliveryCount, bool redelivered, int? redeliveryCount)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SqlTransportMessage message = Message();
        message.DeliveryCount = deliveryCount;
        await using var harness = new ReceiverHarness([message]);

        await harness.Dispatcher.DispatchCompleted.WaitAsync(cancellationToken);
        await harness.Client.SecondReceiveEntered.WaitAsync(cancellationToken);
        await harness.StopAsync(cancellationToken);

        SqlReceiveContext context = Assert.IsType<SqlReceiveContext>(harness.Dispatcher.LastContext);
        Assert.Same(message, context.TransportMessage);
        Assert.Equal(message.TransportMessageId, context.TransportMessageId);
        Assert.Equal(deliveryCount, context.DeliveryCount);
        Assert.Equal(redelivered, context.Redelivered);
        Assert.Equal(redeliveryCount, context.TransportHeaders.Get(MessageHeaders.RedeliveryCount, default(int?)));
        Assert.Equal((1, 1), (harness.Dispatcher.DispatchCount, harness.Client.DeleteCallCount));
        Assert.Equal((0, 0), (harness.Client.MoveCallCount, harness.Client.UnlockCallCount));
        Assert.True(harness.Receiver.Stopped.IsCancellationRequested);
    }

    [Theory]
    [InlineData(false, 1, 0)]
    [InlineData(true, 0, 1)]
    [RequirementCoverage("REQ-VSB-SQL-RECEIVER-LIFECYCLE", "expired-message-uses-configured-terminal-path")]
    public async Task ExpiredMessage_UsesTheConfiguredTerminalPathAsync(bool deadLetterExpiredMessages, int expectedDeletes, int expectedMoves)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SqlTransportMessage message = Message(expirationTime: Now.AddTicks(-1));
        await using var harness = new ReceiverHarness([message], deadLetterExpiredMessages: deadLetterExpiredMessages);

        await harness.Client.SettlementCompleted.WaitAsync(cancellationToken);
        await harness.Client.SecondReceiveEntered.WaitAsync(cancellationToken);
        await harness.StopAsync(cancellationToken);

        Assert.Equal((expectedDeletes, expectedMoves, 0),
            (harness.Client.DeleteCallCount, harness.Client.MoveCallCount, harness.Dispatcher.DispatchCount));
        if (deadLetterExpiredMessages)
        {
            Assert.Equal(SqlQueueType.DeadLetterQueue, harness.Client.LastMoveQueueType);
            Assert.Equal("expired", harness.Client.LastMoveHeaders!.Get(MessageHeaders.Reason, default(string)));
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-RECEIVER-LIFECYCLE", "late-fetch-during-stop-releases-lock")]
    public async Task MessageFetchedAfterStopStarted_IsReleasedForImmediateRedeliveryAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SqlTransportMessage message = Message();
        await using var harness = new ReceiverHarness([message], holdFirstReceive: true);
        harness.Client.HoldUnlock = true;

        await harness.Client.FirstReceiveEntered.WaitAsync(cancellationToken);
        Task stopTask = harness.StopAsync(cancellationToken);
        await WaitForCancellationAsync(harness.Receiver.Stopping, cancellationToken);

        harness.Client.ReleaseFirstReceive();
        await harness.Client.UnlockEntered.WaitAsync(cancellationToken);
        Assert.False(stopTask.IsCompleted);
        harness.Client.ReleaseUnlock();
        await stopTask;

        Assert.Equal(0, harness.Dispatcher.DispatchCount);
        Assert.Equal(1, harness.Client.UnlockCallCount);
        Assert.Equal(TimeSpan.Zero, harness.Client.LastUnlockDelay);
        Assert.Equal(1, harness.Client.LastUnlockHeaders!.Get(MessageHeaders.RedeliveryCount, default(int?)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-RECEIVER-LIFECYCLE", "failed-shutdown-release-is-contained")]
    public async Task FailedShutdownRelease_DoesNotPreventReceiverCompletionAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness = new ReceiverHarness([Message()], holdFirstReceive: true, unlockResult: false);

        await harness.Client.FirstReceiveEntered.WaitAsync(cancellationToken);
        Task stopTask = harness.StopAsync(cancellationToken);
        await WaitForCancellationAsync(harness.Receiver.Stopping, cancellationToken);

        harness.Client.ReleaseFirstReceive();
        await stopTask;

        Assert.Equal((0, 1), (harness.Dispatcher.DispatchCount, harness.Client.UnlockCallCount));
        Assert.True(harness.Receiver.Stopped.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-RECEIVER-LIFECYCLE", "failed-batch-release-continues-with-other-locks")]
    public async Task TimeoutReleasingFirstFetchedMessage_StillReleasesOtherBatchLocksAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness = new ReceiverHarness([Message(), Message()], holdFirstReceive: true, prefetchCount: 2);
        harness.Client.EnqueueUnlockFailure(new TimeoutException("database timed out"));

        await harness.Client.FirstReceiveEntered.WaitAsync(cancellationToken);
        Task stopTask = harness.StopAsync(cancellationToken);
        await WaitForCancellationAsync(harness.Receiver.Stopping, cancellationToken);

        harness.Client.ReleaseFirstReceive();
        await stopTask;

        Assert.Equal((2, 0, 2), (harness.Client.FirstRequestedLimit, harness.Dispatcher.DispatchCount, harness.Client.UnlockCallCount));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("tenant-a")]
    [RequirementCoverage("REQ-VSB-SQL-RECEIVER-LIFECYCLE", "partitioned-mode-dispatches-keyed-and-unkeyed-records")]
    public async Task PartitionedMode_DispatchesKeyedAndUnkeyedRecordsAsync(string? partitionKey)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SqlTransportMessage message = Message();
        message.PartitionKey = partitionKey;
        await using var harness = new ReceiverHarness([message], receiveMode: SqlReceiveMode.Partitioned);

        await harness.Dispatcher.DispatchCompleted.WaitAsync(cancellationToken);
        await harness.Client.SecondReceiveEntered.WaitAsync(cancellationToken);
        await harness.StopAsync(cancellationToken);

        Assert.Equal((message.TransportMessageId, 1, 1),
            (Assert.IsType<SqlReceiveContext>(harness.Dispatcher.LastContext).TransportMessageId,
                harness.Dispatcher.DispatchCount,
                harness.Client.DeleteCallCount));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-RECEIVER-LIFECYCLE", "dispatch-failure-is-settled-and-contained")]
    public async Task DispatchFailure_IsSettledAndDoesNotTerminateTheReceiverAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness = new ReceiverHarness([Message()], failDispatch: true);

        await harness.Dispatcher.DispatchCompleted.WaitAsync(cancellationToken);
        await harness.Client.SecondReceiveEntered.WaitAsync(cancellationToken);
        await harness.StopAsync(cancellationToken);

        Assert.Equal((1, 0), (harness.Client.UnlockCallCount, harness.Client.DeleteCallCount));
        Assert.Equal("fault", harness.Client.LastUnlockHeaders!.Get(MessageHeaders.Reason, default(string)));
        Assert.True(harness.Receiver.Stopped.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-RECEIVER-LIFECYCLE", "polling-without-queue-id-remains-interruptible")]
    public async Task PollingWithoutQueueId_IsStillInterruptedWhenWorkCompletesAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness = new ReceiverHarness([], queueId: null);

        await harness.TimeProvider.TimerCreated.WaitAsync(cancellationToken);
        harness.Receiver.MessageHandled();
        await harness.Client.SecondReceiveEntered.WaitAsync(cancellationToken);
        await harness.StopAsync(cancellationToken);

        Assert.Equal(0, harness.Connection.DelayCallCount);
        Assert.Equal(2, harness.Client.ReceiveCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-RECEIVER-LIFECYCLE", "receive-fault-terminates-owned-lifecycle")]
    public async Task ReceiveFault_TerminatesTheOwnedReceiverLifecycleAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness = new ReceiverHarness([], firstReceiveException: new InvalidOperationException("database unavailable"));

        await harness.Receiver.Completed.WaitAsync(cancellationToken);
        await WaitForCancellationAsync(harness.Receiver.Stopped, cancellationToken);

        Assert.Equal(1, harness.Client.ReceiveCallCount);
        Assert.True(harness.Receiver.Ready.IsCompletedSuccessfully);
    }

    private static async Task WaitForCancellationAsync(CancellationToken observedToken, CancellationToken testToken)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenRegistration registration = observedToken.Register(completion.SetResult);
        if (observedToken.IsCancellationRequested)
            completion.TrySetResult();
        await completion.Task.WaitAsync(testToken);
    }

    private static SqlTransportMessage Message(DateTimeOffset? expirationTime = null)
    {
        return new SqlTransportMessage
        {
            TransportMessageId = NewId.NextGuid(),
            QueueName = "input",
            MessageDeliveryId = 42,
            LockId = NewId.NextGuid(),
            MessageId = NewId.NextGuid(),
            EnqueueTime = Now.AddMinutes(-1),
            ExpirationTime = expirationTime,
            BinaryBody = [1, 2, 3],
            Headers = "[]",
            TransportHeaders = "[]",
        };
    }

    private sealed class ReceiverHarness : IAsyncDisposable
    {
        readonly EndpointContextProxy _endpoint;
        bool _stopped;

        public ReceiverHarness(
            IReadOnlyList<SqlTransportMessage> firstMessages,
            bool deadLetterExpiredMessages = false,
            bool holdFirstReceive = false,
            bool unlockResult = true,
            SqlReceiveMode receiveMode = SqlReceiveMode.Normal,
            bool failDispatch = false,
            long? queueId = 1,
            Exception? firstReceiveException = null,
            TimeSpan? autoDeleteOnIdle = null,
            TimeSpan? pollingInterval = null,
            int prefetchCount = 1)
        {
            ConnectionContext connection = DispatchProxy.Create<ConnectionContext, ConnectionContextProxy>();
            Connection = (ConnectionContextProxy)(object)connection;

            ClientContext client = DispatchProxy.Create<ClientContext, ReceiverClientContextProxy>();
            Client = (ReceiverClientContextProxy)(object)client;
            Client.ConnectionContext = connection;
            Client.FirstMessages = firstMessages;
            Client.FirstReceiveException = firstReceiveException;
            Client.HoldFirstReceive = holdFirstReceive;
            Client.Settings = new TestReceiveSettings(deadLetterExpiredMessages, queueId, receiveMode,
                autoDeleteOnIdle ?? TimeSpan.FromMinutes(2), pollingInterval ?? TimeSpan.FromMinutes(1), prefetchCount);
            Client.UnlockResult = unlockResult;

            IReceivePipeDispatcher dispatcher = DispatchProxy.Create<IReceivePipeDispatcher, ReceivePipeDispatcherProxy>();
            Dispatcher = (ReceivePipeDispatcherProxy)(object)dispatcher;
            Dispatcher.FailDispatch = failDispatch;

            SqlReceiveEndpointContext endpoint = DispatchProxy.Create<SqlReceiveEndpointContext, EndpointContextProxy>();
            _endpoint = (EndpointContextProxy)(object)endpoint;
            _endpoint.Dispatcher = dispatcher;
            _endpoint.PrefetchCount = prefetchCount;
            TimeProvider = new TestTimeProvider(Now);
            _endpoint.TimeProvider = TimeProvider;

            Receiver = new SqlMessageReceiver(client, endpoint);
        }

        public ReceiverClientContextProxy Client { get; }
        public ConnectionContextProxy Connection { get; }
        public ReceivePipeDispatcherProxy Dispatcher { get; }
        public SqlMessageReceiver Receiver { get; }
        public TestTimeProvider TimeProvider { get; }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_stopped)
                return;

            await Receiver.StopAsync("test completed", cancellationToken);
            _stopped = true;
        }

        public async ValueTask DisposeAsync()
        {
            Client.ReleaseFirstReceive();
            Client.ReleaseUnlock();
            if (!_stopped)
                await Receiver.StopAsync("test cleanup", CancellationToken.None);
        }
    }

    private class EndpointContextProxy : DispatchProxy
    {
        public IReceivePipeDispatcher Dispatcher { get; set; } = null!;
        public int PrefetchCount { get; set; } = 1;
        public TimeProvider TimeProvider { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name switch
            {
                "CreateReceivePipeDispatcher" => Dispatcher,
                "get_InputAddress" => new Uri("db://localhost/transport/input"),
                "get_PrefetchCount" => PrefetchCount,
                "get_ConcurrentMessageLimit" => PrefetchCount,
                "get_ConsumerStopTimeout" => null,
                "get_StopTimeout" => null,
                "get_LogContext" => null,
                "TryGetPayload" => TryGetPayload(targetMethod, args!),
                _ => throw new NotSupportedException(targetMethod.Name),
            };
        }

        object TryGetPayload(MethodInfo targetMethod, object?[] args)
        {
            if (targetMethod.GetGenericArguments()[0] == typeof(TimeProvider))
            {
                args[0] = TimeProvider;
                return true;
            }

            args[0] = null;
            return false;
        }
    }

    private class ReceivePipeDispatcherProxy : DispatchProxy
    {
        ZeroActivityHandler? _zeroActivity;
        int _activeDispatchCount;
        long _dispatchCount;

        readonly TaskCompletionSource _dispatchCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int DispatchCount => checked((int)Interlocked.Read(ref _dispatchCount));
        public Task DispatchCompleted => _dispatchCompleted.Task;
        public bool FailDispatch { get; set; }
        public ReceiveContext? LastContext { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name switch
            {
                "add_ZeroActivity" => AddZeroActivity(args!),
                "remove_ZeroActivity" => RemoveZeroActivity(args!),
                "get_ActiveDispatchCount" => Volatile.Read(ref _activeDispatchCount),
                "get_DispatchCount" => Interlocked.Read(ref _dispatchCount),
                "get_MaxConcurrentDispatchCount" => Volatile.Read(ref _activeDispatchCount),
                "DispatchAsync" => DispatchAsync(args!),
                _ => throw new NotSupportedException(targetMethod.Name),
            };
        }

        object? AddZeroActivity(object?[] args)
        {
            _zeroActivity += (ZeroActivityHandler)args[0]!;
            return null;
        }

        async Task DispatchAsync(object?[] args)
        {
            LastContext = (ReceiveContext)args[0]!;
            var receiveLock = (ReceiveLockContext)args[1]!;
            var cancellationToken = (CancellationToken)args[2]!;
            Interlocked.Increment(ref _activeDispatchCount);
            Interlocked.Increment(ref _dispatchCount);

            try
            {
                if (FailDispatch)
                {
                    var exception = new InvalidOperationException("consumer pipeline failed");
                    await receiveLock.FaultedAsync(exception, cancellationToken);
                    throw exception;
                }

                await receiveLock.CompleteAsync(cancellationToken);
            }
            finally
            {
                _dispatchCompleted.TrySetResult();
                if (Interlocked.Decrement(ref _activeDispatchCount) == 0 && _zeroActivity is not null)
                    await _zeroActivity();
            }
        }

        object? RemoveZeroActivity(object?[] args)
        {
            _zeroActivity -= (ZeroActivityHandler)args[0]!;
            return null;
        }
    }

    private class ReceiverClientContextProxy : DispatchProxy
    {
        readonly TaskCompletionSource _firstReceiveEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _releaseFirstReceive = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _secondReceiveEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _settlementCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _unlockEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _releaseUnlock = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly Queue<Exception> _unlockFailures = new();
        int _receiveCallCount;

        public ConnectionContext ConnectionContext { get; set; } = null!;
        public int DeadLetterCallCount { get; private set; }
        public int DeleteCallCount { get; private set; }
        public Task FirstReceiveEntered => _firstReceiveEntered.Task;
        public int FirstRequestedLimit { get; private set; }
        public Exception? FirstReceiveException { get; set; }
        public IReadOnlyList<SqlTransportMessage> FirstMessages { get; set; } = [];
        public bool HoldFirstReceive { get; set; }
        public bool HoldUnlock { get; set; }
        public SendHeaders? LastMoveHeaders { get; private set; }
        public SqlQueueType? LastMoveQueueType { get; private set; }
        public CancellationToken LastReceiveCancellationToken { get; private set; }
        public TimeSpan? LastUnlockDelay { get; private set; }
        public SendHeaders? LastUnlockHeaders { get; private set; }
        public int MoveCallCount { get; private set; }
        public Action? OnDeadLetter { get; set; }
        public int ReceiveCallCount => Volatile.Read(ref _receiveCallCount);
        public Task SecondReceiveEntered => _secondReceiveEntered.Task;
        public Task SettlementCompleted => _settlementCompleted.Task;
        public ReceiveSettings Settings { get; set; } = null!;
        public int TouchCallCount { get; private set; }
        public int UnlockCallCount { get; private set; }
        public bool UnlockResult { get; set; } = true;
        public Task UnlockEntered => _unlockEntered.Task;

        public void ReleaseFirstReceive() => _releaseFirstReceive.TrySetResult();
        public void ReleaseUnlock() => _releaseUnlock.TrySetResult();
        public void EnqueueUnlockFailure(Exception exception) => _unlockFailures.Enqueue(exception);

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name switch
            {
                "get_CancellationToken" => CancellationToken.None,
                "get_ConnectionContext" => ConnectionContext,
                "TryGetPayload" => TryGetPayload(targetMethod, args!),
                "ReceiveMessagesAsync" => ReceiveMessagesAsync(args!),
                "DeadLetterQueueAsync" => DeadLetterQueueAsync(),
                "TouchQueueAsync" => TouchQueueAsync(),
                "DeleteMessageAsync" => DeleteMessageAsync(),
                "MoveMessageAsync" => MoveMessageAsync(args!),
                "UnlockAsync" => UnlockAsync(args!),
                "RenewLockAsync" => Task.FromResult(true),
                _ => throw new NotSupportedException(targetMethod.Name),
            };
        }

        Task<int?> DeadLetterQueueAsync()
        {
            DeadLetterCallCount++;
            OnDeadLetter?.Invoke();
            return Task.FromResult<int?>(0);
        }

        Task<bool> DeleteMessageAsync()
        {
            DeleteCallCount++;
            _settlementCompleted.TrySetResult();
            return Task.FromResult(true);
        }

        Task<bool> MoveMessageAsync(object?[] args)
        {
            MoveCallCount++;
            LastMoveQueueType = (SqlQueueType)args[3]!;
            LastMoveHeaders = (SendHeaders)args[5]!;
            _settlementCompleted.TrySetResult();
            return Task.FromResult(true);
        }

        async Task<IEnumerable<SqlTransportMessage>> ReceiveMessagesAsync(object?[] args)
        {
            int callCount = Interlocked.Increment(ref _receiveCallCount);
            LastReceiveCancellationToken = (CancellationToken)args[5]!;

            if (callCount == 1)
            {
                FirstRequestedLimit = (int)args[2]!;
                _firstReceiveEntered.TrySetResult();
                if (FirstReceiveException is not null)
                    throw FirstReceiveException;
                if (HoldFirstReceive)
                    await _releaseFirstReceive.Task;
                return FirstMessages;
            }

            _secondReceiveEntered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, LastReceiveCancellationToken);
            return [];
        }

        Task TouchQueueAsync()
        {
            TouchCallCount++;
            return Task.CompletedTask;
        }

        object TryGetPayload(MethodInfo targetMethod, object?[] args)
        {
            if (targetMethod.GetGenericArguments()[0] == typeof(ReceiveSettings))
            {
                args[0] = Settings;
                return true;
            }

            args[0] = null;
            return false;
        }

        async Task<bool> UnlockAsync(object?[] args)
        {
            UnlockCallCount++;
            LastUnlockDelay = (TimeSpan)args[2]!;
            LastUnlockHeaders = (SendHeaders)args[3]!;
            _unlockEntered.TrySetResult();
            if (HoldUnlock)
                await _releaseUnlock.Task;
            _settlementCompleted.TrySetResult();
            if (_unlockFailures.TryDequeue(out Exception? exception))
                throw exception;
            return UnlockResult;
        }
    }

    private class ConnectionContextProxy : DispatchProxy
    {
        readonly TaskCompletionSource _delayEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int _delayCallCount;

        public int DelayCallCount => Volatile.Read(ref _delayCallCount);
        public Task DelayEntered => _delayEntered.Task;
        public TimeSpan LastPollingInterval { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name != "DelayUntilMessageReadyAsync")
                throw new NotSupportedException(targetMethod.Name);

            Interlocked.Increment(ref _delayCallCount);
            LastPollingInterval = (TimeSpan)args![1]!;
            _delayEntered.TrySetResult();
            return DelayUntilCanceledAsync((CancellationToken)args![3]!);
        }

        static async Task DelayUntilCanceledAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }

    public sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        readonly TaskCompletionSource _timerCreated = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task TimerCreated => _timerCreated.Task;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            _timerCreated.TrySetResult();
            return TimeProvider.System.CreateTimer(callback, state, dueTime, period);
        }

        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class TestReceiveSettings(bool deadLetterExpiredMessages, long? queueId, SqlReceiveMode receiveMode,
        TimeSpan autoDeleteOnIdle, TimeSpan pollingInterval, int prefetchCount) : ReceiveSettings
    {
        public string QueueName => "input";
        public TimeSpan? AutoDeleteOnIdle => autoDeleteOnIdle;
        public int? MaxDeliveryCount => 10;
        public long? QueueId => queueId;
        public int PrefetchCount => prefetchCount;
        public int ConcurrentMessageLimit => prefetchCount;
        public int ConcurrentDeliveryLimit => prefetchCount;
        public SqlReceiveMode ReceiveMode => receiveMode;
        public bool PurgeOnStartup => false;
        public TimeSpan LockDuration => TimeSpan.FromHours(1);
        public TimeSpan PollingInterval => pollingInterval;
        public TimeSpan? UnlockDelay => null;
        public TimeSpan MaxLockDuration => TimeSpan.FromHours(2);
        public string EntityName => QueueName;
        public int MaintenanceBatchSize => 100;
        public bool DeadLetterExpiredMessages => deadLetterExpiredMessages;
    }
}
