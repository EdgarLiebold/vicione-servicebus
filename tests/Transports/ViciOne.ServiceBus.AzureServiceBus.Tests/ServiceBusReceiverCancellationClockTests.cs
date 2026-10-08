using System.Collections.Concurrent;
using System.Reflection;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusReceiverCancellationClockTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Grace = TimeSpan.FromSeconds(17);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "processor-grace-uses-selected-clock-and-disposes-timer")]
    public Task ProcessorCancellation_UsesSelectedClockForGraceAndDisposesTimerAsync() => CancellationAsync(false, Grace);

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "session-grace-uses-selected-clock-and-disposes-timer")]
    public Task SessionCancellation_UsesSelectedClockForGraceAndDisposesTimerAsync() => CancellationAsync(true, Grace);

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "processor-without-grace-cancels-immediately")]
    public Task ProcessorCancellation_WithoutGraceCancelsImmediatelyAsync() => CancellationAsync(false, null);

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "session-without-grace-cancels-immediately")]
    public Task SessionCancellation_WithoutGraceCancelsImmediatelyAsync() => CancellationAsync(true, null);

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "processor-completion-removes-live-broker-registration")]
    public Task ProcessorCompletion_RemovesBrokerCancellationRegistrationByCallbackCompletionAsync() => LateCancellationAsync(false);

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "session-completion-removes-live-broker-registration")]
    public Task SessionCompletion_RemovesBrokerCancellationRegistrationByCallbackCompletionAsync() => LateCancellationAsync(true);

    private static async Task CancellationAsync(bool session, TimeSpan? grace)
    {
        using var broker = new CancellationTokenSource();
        var fixture = new Fixture(session, grace);
        Task? callback = null;
        try
        {
            fixture.Receiver.Start();
            await fixture.Receiver.Ready.WaitAsync(Timeout, TestToken);
            Assert.True(broker.Token.CanBeCanceled);
            Assert.False(broker.IsCancellationRequested);
            callback = fixture.InvokeCallback(broker.Token);
            await fixture.DispatchEntered.Task.WaitAsync(Timeout, TestToken);
            fixture.AssertHeldDelivery(callback);
            Assert.Empty(fixture.Clock.Timers);
            Assert.False(fixture.DeliveryToken.IsCancellationRequested);

            // Cancel is synchronous: the registered real receiver callback has returned before observations.
            broker.Cancel();
            if (grace.HasValue)
            {
                RecordingTimer timer = Assert.Single(fixture.Clock.Timers);
                Assert.Equal(grace.Value, timer.DueTime);
                Assert.Equal(System.Threading.Timeout.InfiniteTimeSpan, timer.Period);
                Assert.False(timer.ProductDisposed);
                Assert.False(fixture.DeliveryToken.IsCancellationRequested);
                fixture.Clock.Advance(grace.Value - TimeSpan.FromTicks(1));
                Assert.False(fixture.DeliveryToken.IsCancellationRequested);
                fixture.Clock.Advance(TimeSpan.FromTicks(1));
                Assert.True(fixture.DeliveryToken.IsCancellationRequested);
            }
            else
            {
                Assert.Empty(fixture.Clock.Timers);
                Assert.True(fixture.DeliveryToken.IsCancellationRequested);
            }
            fixture.AssertHeldDelivery(callback);
            fixture.DispatchRelease.TrySetResult();
            await fixture.SettlementEntered.Task.WaitAsync(Timeout, TestToken);
            Assert.Equal(1, fixture.CompleteCalls);
            Assert.Same(fixture.Message, fixture.SettledMessage);
            Assert.Equal(fixture.Receiver.Stopped, fixture.SettlementToken);
            Assert.False(callback.IsCompleted);
            Assert.False(fixture.ActualDispatch!.IsCompleted);
            fixture.SettlementRelease.TrySetResult();
            await callback.WaitAsync(Timeout, TestToken);
            Assert.True(callback.IsCompletedSuccessfully);
            Assert.True(fixture.ActualDispatch!.IsCompletedSuccessfully);
            Assert.Equal(1, fixture.CompleteCalls);
            Assert.Throws<ObjectDisposedException>(() => { _ = fixture.Context!.CancellationToken; });
            Assert.Equal(grace.HasValue ? 1 : 0, fixture.Clock.Timers.Count);
            // Observe product disposal before the fixture cleans up timers from hostile controls.
            Assert.All(fixture.Clock.Timers, timer => Assert.True(timer.ProductDisposed));
        }
        finally
        {
            await fixture.DrainAsync(callback);
        }
        fixture.AssertStoppedAndDisposed();
    }

    private static async Task LateCancellationAsync(bool session)
    {
        using var broker = new CancellationTokenSource();
        var fixture = new Fixture(session, Grace);
        Task? callback = null;
        try
        {
            fixture.Receiver.Start();
            await fixture.Receiver.Ready.WaitAsync(Timeout, TestToken);
            Assert.True(broker.Token.CanBeCanceled);
            Assert.False(broker.IsCancellationRequested);
            callback = fixture.InvokeCallback(broker.Token);
            await fixture.DispatchEntered.Task.WaitAsync(Timeout, TestToken);
            fixture.AssertHeldDelivery(callback);
            fixture.DispatchRelease.TrySetResult();
            await fixture.SettlementEntered.Task.WaitAsync(Timeout, TestToken);
            Assert.Equal(1, fixture.CompleteCalls);
            Assert.Same(fixture.Message, fixture.SettledMessage);
            Assert.Equal(fixture.Receiver.Stopped, fixture.SettlementToken);
            Assert.False(callback.IsCompleted);
            Assert.False(fixture.ActualDispatch!.IsCompleted);
            fixture.SettlementRelease.TrySetResult();
            await callback.WaitAsync(Timeout, TestToken);
            Assert.True(callback.IsCompletedSuccessfully);
            Assert.True(fixture.ActualDispatch!.IsCompletedSuccessfully);
            Assert.Equal(1, fixture.CompleteCalls);
            Assert.False(fixture.DeliveryToken.IsCancellationRequested);
            Assert.Throws<ObjectDisposedException>(() => { _ = fixture.Context!.CancellationToken; });
            Assert.Empty(fixture.Clock.Timers);

            Assert.False(broker.IsCancellationRequested);
            // The token is still uncanceled here; this reaches any incorrectly retained SDK registration.
            Assert.Null(Record.Exception(() => broker.Cancel()));
            Assert.True(broker.IsCancellationRequested);
            Assert.Empty(fixture.Clock.Timers);
            fixture.Clock.Advance(Grace);
            Assert.False(fixture.DeliveryToken.IsCancellationRequested);
        }
        finally
        {
            await fixture.DrainAsync(callback);
        }
        fixture.AssertStoppedAndDisposed();
    }

    private sealed class Fixture
    {
        private ZeroActivityHandler? _zeroActivity;
        private Func<ProcessMessageEventArgs, ServiceBusReceivedMessage, CancellationToken, Task>? _messageCallback;
        private Func<ProcessSessionMessageEventArgs, ServiceBusReceivedMessage, CancellationToken, Task>? _sessionCallback;
        private readonly bool _session;
        private readonly ServiceBusReceiver _sdk;
        private CancellationTokenSource? _sdkExpiry;
        private int _active;
        private int _sdkDisposed;
        private int _shutdown;
        private int _close;

        public Fixture(bool session, TimeSpan? grace)
        {
            _session = session;
            // SDK7.20.2's public non-session mock owns a SYSTEM expiry timer. One real origin keeps it valid;
            // the deadline under test uses only the selected fake clock and is not paced by wall time.
            DateTimeOffset origin = DateTimeOffset.UtcNow;
            Clock = new RecordingTimeProvider(origin);
            Message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("clock-owned-delivery"),
                messageId: "clock-owned", sessionId: session ? "clock-session" : null, sequenceNumber: 719L,
                enqueuedTime: origin, timeToLive: TimeSpan.FromHours(1), lockedUntil: origin.AddMinutes(30));
            _sdk = session ? new RecordingSessionReceiver(this) : new RecordingReceiver(this);
            var dispatcher = Proxy<IReceivePipeDispatcher>(DispatchMember);
            var endpoint = Proxy<ServiceBusReceiveEndpointContext>((method, args) => method.Name switch
            {
                "CreateReceivePipeDispatcher" => dispatcher,
                "get_InputAddress" => InputAddress,
                "get_LogContext" or "get_StopTimeout" => null,
                "get_ConsumerStopTimeout" => grace,
                "TryGetPayload" => ProvideClock(method, args),
                "HasPayloadType" => false,
                _ => throw new NotSupportedException(method.Name)
            });
            var client = Proxy<ClientContext>((method, args) =>
            {
                switch (method.Name)
                {
                    case "ConfigureMessageProcessor":
                        _messageCallback = (Func<ProcessMessageEventArgs, ServiceBusReceivedMessage, CancellationToken, Task>)args[0]!;
                        return null;
                    case "ConfigureSessionProcessor":
                        _sessionCallback = (Func<ProcessSessionMessageEventArgs, ServiceBusReceivedMessage, CancellationToken, Task>)args[0]!;
                        return null;
                    case "StartAsync": return Task.CompletedTask;
                    case "ShutdownAsync": _shutdown++; return Task.CompletedTask;
                    case "CloseAsync": _close++; return Task.CompletedTask;
                    case "get_InputAddress": return InputAddress;
                    default: throw new NotSupportedException(method.Name);
                }
            });
            Receiver = session ? new SessionReceiver(client, endpoint) : new Receiver(client, endpoint);
        }

        public Uri InputAddress { get; } = new("sb://clock-owned.servicebus.invalid/input");
        public RecordingTimeProvider Clock { get; }
        public Receiver Receiver { get; }
        public ServiceBusReceivedMessage Message { get; }
        public ServiceBusReceiveContext? Context { get; private set; }
        public CancellationToken DeliveryToken { get; private set; }
        public Task? ActualDispatch { get; private set; }
        public int CompleteCalls { get; private set; }
        public ServiceBusReceivedMessage? SettledMessage { get; private set; }
        public CancellationToken SettlementToken { get; private set; }
        public TaskCompletionSource DispatchEntered { get; } = NewSignal();
        public TaskCompletionSource DispatchRelease { get; } = NewSignal();
        public TaskCompletionSource SettlementEntered { get; } = NewSignal();
        public TaskCompletionSource SettlementRelease { get; } = NewSignal();

        public Task InvokeCallback(CancellationToken token)
        {
            if (_session)
            {
                Assert.Null(_messageCallback);
                return (_sessionCallback ?? throw new InvalidOperationException("No session callback registered"))(
                    new ProcessSessionMessageEventArgs(Message, (ServiceBusSessionReceiver)_sdk, token), Message, token);
            }
            Assert.Null(_sessionCallback);
            var args = new ProcessMessageEventArgs(Message, _sdk, token);
            _sdkExpiry = (CancellationTokenSource)(typeof(ProcessMessageEventArgs)
                .GetProperty("MessageLockLostCancellationSource", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(args)
                ?? throw new InvalidOperationException("Pinned SDK mock expiry owner not available"));
            return (_messageCallback ?? throw new InvalidOperationException("No processor callback registered"))(args, Message, token);
        }

        private object ProvideClock(MethodInfo method, object?[] args)
        {
            bool selected = method.GetGenericArguments()[0] == typeof(TimeProvider);
            args[0] = selected ? Clock : null;
            return selected;
        }

        public void AssertHeldDelivery(Task callback)
        {
            Assert.IsType<ServiceBusReceiveContext>(Context);
            Assert.Equal("clock-owned", Context!.MessageId);
            Assert.Equal(719L, Context.SequenceNumber);
            Assert.Equal(InputAddress, Context.InputAddress);
            Assert.False(callback.IsCompleted);
            Assert.NotNull(ActualDispatch);
            Assert.False(ActualDispatch.IsCompleted);
            Assert.False(DispatchRelease.Task.IsCompleted);
            Assert.False(SettlementEntered.Task.IsCompleted);
        }

        private object? DispatchMember(MethodInfo method, object?[] args)
        {
            switch (method.Name)
            {
                case "add_ZeroActivity": _zeroActivity += (ZeroActivityHandler)args[0]!; return null;
                case "remove_ZeroActivity": _zeroActivity -= (ZeroActivityHandler)args[0]!; return null;
                case "get_ActiveDispatchCount": return _active;
                case "get_DispatchCount": return 1L;
                case "get_MaxConcurrentDispatchCount": return 1;
                case "DispatchAsync":
                    Context = Assert.IsType<ServiceBusReceiveContext>(args[0]);
                    DeliveryToken = Context.CancellationToken;
                    Assert.Equal(CancellationToken.None, Assert.IsType<CancellationToken>(args[2]));
                    _active++;
                    ActualDispatch = PumpAsync(Assert.IsAssignableFrom<ReceiveLockContext>(args[1]));
                    return ActualDispatch;
                default: throw new NotSupportedException(method.Name);
            }
        }

        private async Task PumpAsync(ReceiveLockContext receiveLock)
        {
            try
            {
                await receiveLock.ValidateLockStatusAsync(CancellationToken.None).ConfigureAwait(false);
                DispatchEntered.TrySetResult();
                await DispatchRelease.Task.ConfigureAwait(false);
                await receiveLock.CompleteAsync(CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                _active--;
                if (_zeroActivity is not null)
                    foreach (ZeroActivityHandler handler in _zeroActivity.GetInvocationList())
                        await handler().ConfigureAwait(false);
            }
        }

        private Task CompleteAsync(ServiceBusReceivedMessage message, CancellationToken token)
        {
            CompleteCalls++;
            SettledMessage = message;
            SettlementToken = token;
            SettlementEntered.TrySetResult();
            return SettlementRelease.Task;
        }

        public async Task DrainAsync(Task? callback)
        {
            DispatchRelease.TrySetResult();
            SettlementRelease.TrySetResult();
            try
            {
                try { if (callback is not null) await callback.WaitAsync(Timeout, CancellationToken.None); }
                finally
                {
                    try { if (ActualDispatch is not null) await ActualDispatch.WaitAsync(Timeout, CancellationToken.None); }
                    finally
                    {
                        try { await Receiver.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None); }
                        finally { await Receiver.Completed.WaitAsync(Timeout, CancellationToken.None); }
                    }
                }
            }
            finally
            {
                try { _sdkExpiry?.Dispose(); }
                finally
                {
                    try { await _sdk.DisposeAsync(); }
                    finally { Clock.CleanupFixtureTimers(); }
                }
            }
        }

        public void AssertStoppedAndDisposed()
        {
            Assert.Equal(0, _active);
            Assert.Equal(1, _shutdown);
            Assert.Equal(1, _close);
            Assert.True(Receiver.Completed.IsCompletedSuccessfully);
            Assert.True(Receiver.Stopped.IsCancellationRequested);
            Assert.Equal(1, _sdkDisposed);
        }

        private sealed class RecordingReceiver(Fixture owner) : ServiceBusReceiver
        {
            public override Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default) => owner.CompleteAsync(message, cancellationToken);
            public override ValueTask DisposeAsync() { owner._sdkDisposed++; return ValueTask.CompletedTask; }
        }

        private sealed class RecordingSessionReceiver(Fixture owner) : ServiceBusSessionReceiver
        {
            public override Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default) => owner.CompleteAsync(message, cancellationToken);
            public override ValueTask DisposeAsync() { owner._sdkDisposed++; return ValueTask.CompletedTask; }
        }
    }

    private sealed class RecordingTimeProvider(DateTimeOffset origin) : TimeProvider
    {
        private readonly FakeTimeProvider _clock = new(origin);
        public ConcurrentQueue<RecordingTimer> Timers { get; } = new();
        public override DateTimeOffset GetUtcNow() => _clock.GetUtcNow();
        public override long GetTimestamp() => _clock.GetTimestamp();
        public override long TimestampFrequency => _clock.TimestampFrequency;
        public void Advance(TimeSpan delta) => _clock.Advance(delta);
        public void CleanupFixtureTimers()
        {
            foreach (RecordingTimer timer in Timers) timer.CleanupFromFixture();
        }
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new RecordingTimer(_clock.CreateTimer(callback, state, dueTime, period), dueTime, period);
            Timers.Enqueue(timer);
            return timer;
        }
    }

    private sealed class RecordingTimer(ITimer timer, TimeSpan dueTime, TimeSpan period) : ITimer
    {
        private int _disposed;
        public TimeSpan DueTime { get; } = dueTime;
        public TimeSpan Period { get; } = period;
        public bool ProductDisposed => Volatile.Read(ref _disposed) != 0;
        public bool Change(TimeSpan dueTime, TimeSpan period) => timer.Change(dueTime, period);
        public void Dispose() { if (Interlocked.Exchange(ref _disposed, 1) == 0) timer.Dispose(); }
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        // This releases fixture-owned scheduling resources without manufacturing a product-disposal observation.
        public void CleanupFromFixture() => timer.Dispose();
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static T Proxy<T>(Func<MethodInfo, object?[], object?> handler) where T : class
    {
        T proxy = DispatchProxy.Create<T, ContextProxy>();
        ((ContextProxy)(object)proxy).Handler = handler;
        return proxy;
    }
    public class ContextProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[], object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(
            targetMethod ?? throw new InvalidOperationException("Missing proxy method"), args ?? []);
    }
}
