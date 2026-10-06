using System.Reflection;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusReceiverDispatchOwnershipTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [RequirementCoverage("REQ-VSB-ASB-FUNCTIONS-TESTING", "trigger-held-dispatch-outcome-and-context-registration-cleanup")]
    public async Task TriggerHandle_JoinsActualDispatchPreservesItsOutcomeAndDisposesTheDeliveryAsync(int outcome)
    {
        Exception failure = outcome switch
        {
            2 => new ServiceBusException(false, "unique message lock lost", "receiver-owned", ServiceBusFailureReason.MessageLockLost, null),
            3 => new ServiceBusException(false, "unique session lock lost", "receiver-owned", ServiceBusFailureReason.SessionLockLost, null),
            _ => new InvalidOperationException("unique trigger dispatch failure")
        };
        using var caller = new CancellationTokenSource();
        var fixture = new Fixture(failure);
        Task? pending = null;
        try
        {
            var receiver = new ServiceBusMessageReceiver(fixture.Endpoint);
            pending = receiver.HandleAsync(fixture.Message, caller.Token);
            await fixture.DispatchEntered.Task.WaitAsync(Timeout, TestToken);
            ServiceBusReceiveContext context = fixture.AssertActualContext();
            Assert.Same(NoLockReceiveContext.Instance, fixture.Lock);
            Assert.Equal(caller.Token, fixture.DispatchToken);
            Assert.False(pending.IsCompleted);
            Assert.False(fixture.ActualDispatch!.IsCompleted);
            Assert.False(fixture.DeliveryToken.IsCancellationRequested);
            if (outcome == 4)
            {
                caller.Cancel();
                Assert.True(fixture.DeliveryToken.IsCancellationRequested);
                Assert.False(pending.IsCompleted);
                fixture.DispatchRelease.TrySetCanceled(caller.Token);
            }
            else if (outcome == 0) fixture.DispatchRelease.TrySetResult();
            else fixture.DispatchRelease.TrySetException(failure);

            Exception? observed = await Record.ExceptionAsync(() => pending.WaitAsync(Timeout, TestToken));
            if (outcome == 0)
            {
                Assert.Null(observed);
                Assert.True(pending.IsCompletedSuccessfully);
            }
            else if (outcome == 4)
            {
                Assert.Equal(caller.Token, Assert.IsAssignableFrom<OperationCanceledException>(observed).CancellationToken);
                Assert.True(pending.IsCanceled);
            }
            else
            {
                Assert.Same(failure, observed);
                Assert.True(pending.IsFaulted);
            }
            Assert.True(fixture.ActualDispatch!.IsCompleted);
            Assert.Equal(0, fixture.Sdk.CompleteCalls);
            Assert.Throws<ObjectDisposedException>(() => { _ = context.CancellationToken; });
            // The owned registration must be gone before callers can cancel a disposed context.
            Assert.Null(Record.Exception(() => caller.Cancel()));
            if (outcome != 4) Assert.False(fixture.DeliveryToken.IsCancellationRequested);
        }
        finally
        {
            await fixture.DrainAsync(pending, null, null);
        }
        Assert.Equal(1, fixture.Sdk.DisposeCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "sdk-callback-joins-real-azure-lock-dispatch-and-owns-terminal-errors")]
    public async Task ProcessorCallback_JoinsTheRealLockDispatchAndOwnsFailuresBeforeDisposingTheContextAsync(int outcome)
    {
        using var caller = new CancellationTokenSource();
        var fixture = new Fixture(new InvalidOperationException("unique processor dispatcher failure"));
        var receiver = new Receiver(fixture.Client, fixture.Endpoint);
        Task? pending = null;
        try
        {
            receiver.Start();
            await receiver.Ready.WaitAsync(Timeout, TestToken);
            pending = fixture.StartCallbackAsync(caller.Token);
            await fixture.DispatchEntered.Task.WaitAsync(Timeout, TestToken);
            ServiceBusReceiveContext context = fixture.AssertActualContext();
            Assert.IsAssignableFrom<ReceiveLockContext>(fixture.Lock);
            Assert.NotSame(NoLockReceiveContext.Instance, fixture.Lock);
            Assert.Equal(CancellationToken.None, fixture.DispatchToken);
            Assert.False(pending.IsCompleted);
            Assert.False(fixture.ActualDispatch!.IsCompleted);
            Assert.Equal(0, fixture.Sdk.CompleteCalls);

            if (outcome == 0)
            {
                fixture.DispatchRelease.TrySetResult();
                await fixture.Sdk.CompleteEntered.Task.WaitAsync(Timeout, TestToken);
                Assert.Same(fixture.Message, fixture.Sdk.Message);
                Assert.Equal(receiver.Stopped, fixture.Sdk.Token);
                Assert.False(pending.IsCompleted);
                Assert.False(fixture.ActualDispatch!.IsCompleted);
                Assert.False(fixture.Sdk.CompleteRelease.Task.IsCompleted);
                fixture.Sdk.CompleteRelease.TrySetResult();
            }
            else if (outcome == 1) fixture.DispatchRelease.TrySetException(fixture.Failure);
            else
            {
                caller.Cancel();
                Assert.True(fixture.DeliveryToken.IsCancellationRequested);
                Assert.False(pending.IsCompleted);
                fixture.DispatchRelease.TrySetCanceled(caller.Token);
            }
            // Azure owns the processor callback: failures do not escape to its SDK pump.
            await pending.WaitAsync(Timeout, TestToken);
            Assert.True(pending.IsCompletedSuccessfully);
            Assert.Equal(outcome == 0 ? 1 : 0, fixture.Sdk.CompleteCalls);
            if (outcome == 1)
                Assert.Same(fixture.Failure, Assert.Single(fixture.ActualDispatch!.Exception!.Flatten().InnerExceptions));
            if (outcome == 2) Assert.True(fixture.ActualDispatch!.IsCanceled);
            Assert.Equal(outcome != 0, fixture.DeliveryToken.IsCancellationRequested);
            Assert.Throws<ObjectDisposedException>(() => { _ = context.CancellationToken; });
            Assert.Null(Record.Exception(() => caller.Cancel()));
        }
        finally
        {
            await fixture.DrainAsync(pending, receiver, null);
        }
        Assert.Equal(1, fixture.Sdk.DisposeCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "receiver-stop-orders-shutdown-active-sdk-settlement-and-close-with-uncanceled-budget")]
    public async Task ReceiverStop_JoinsShutdownActiveDeliveryAndCloseInThatOrderAsync()
    {
        var fixture = new Fixture(new InvalidOperationException("unused shutdown control failure"));
        var receiver = new Receiver(fixture.Client, fixture.Endpoint);
        Task? delivery = null;
        Task? stop = null;
        try
        {
            receiver.Start();
            await receiver.Ready.WaitAsync(Timeout, TestToken);
            delivery = fixture.StartCallbackAsync(CancellationToken.None);
            await fixture.DispatchEntered.Task.WaitAsync(Timeout, TestToken);
            fixture.ExpectStopIdleRead = true;
            stop = receiver.StopAsync(CancellationToken.None);
            await fixture.ShutdownEntered.Task.WaitAsync(Timeout, TestToken);
            Assert.True(receiver.Stopping.IsCancellationRequested);
            Assert.False(stop.IsCompleted);
            Assert.False(delivery.IsCompleted);
            Assert.False(fixture.ShutdownRelease.Task.IsCompleted);
            Assert.Equal(0, fixture.CloseCalls);
            fixture.ShutdownRelease.TrySetResult();
            // Observe the actual base-consumer readiness check after Azure shutdown was awaited.
            await fixture.StopIdleRead.Task.WaitAsync(Timeout, TestToken);
            Assert.Equal(0, fixture.CloseCalls);
            Assert.False(stop.IsCompleted);
            Assert.False(receiver.Completed.IsCompleted);
            fixture.DispatchRelease.TrySetResult();
            await fixture.Sdk.CompleteEntered.Task.WaitAsync(Timeout, TestToken);
            Assert.False(delivery.IsCompleted);
            Assert.False(stop.IsCompleted);
            Assert.Equal(0, fixture.CloseCalls);
            fixture.Sdk.CompleteRelease.TrySetResult();
            await fixture.CloseEntered.Task.WaitAsync(Timeout, TestToken);
            Assert.Equal(0, fixture.ActiveCount);
            Assert.False(stop.IsCompleted);
            Assert.False(receiver.Completed.IsCompleted);
            Assert.False(fixture.CloseRelease.Task.IsCompleted);
            fixture.CloseRelease.TrySetResult();
            await stop.WaitAsync(Timeout, TestToken);
            await receiver.Completed.WaitAsync(Timeout, TestToken);
            await delivery.WaitAsync(Timeout, TestToken);
            Assert.Equal(1, fixture.ShutdownCalls);
            Assert.Equal(1, fixture.CloseCalls);
            Assert.Equal(1, fixture.Sdk.CompleteCalls);
            Assert.True(receiver.Stopped.IsCancellationRequested);
        }
        finally
        {
            await fixture.DrainAsync(delivery, receiver, stop);
        }
        Assert.Equal(1, fixture.Sdk.DisposeCalls);
    }

    private sealed class Fixture
    {
        private ZeroActivityHandler? _zeroActivity;
        private Func<ProcessMessageEventArgs, ServiceBusReceivedMessage, CancellationToken, Task>? _callback;
        public Fixture(Exception failure)
        {
            Failure = failure;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Message = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromBytes(new byte[] { 5, 10, 17 }),
                messageId: "receiver-owned-message", sequenceNumber: 913L, deliveryCount: 2,
                timeToLive: TimeSpan.FromHours(1), enqueuedTime: now, lockedUntil: now.AddMinutes(30));
            Sdk = new RecordingReceiver();
            IReceivePipeDispatcher dispatcher = Proxy<IReceivePipeDispatcher>(DispatchMember);
            Endpoint = Proxy<ServiceBusReceiveEndpointContext>((method, args) => method.Name switch
            {
                "CreateReceivePipeDispatcher" => dispatcher,
                "get_InputAddress" => InputAddress,
                "get_LogContext" or "get_StopTimeout" or "get_ConsumerStopTimeout" => null,
                "TryGetPayload" => NoPayload(args),
                "HasPayloadType" => false,
                _ => throw new NotSupportedException(method.Name)
            });
            Client = Proxy<ClientContext>((method, args) =>
            {
                switch (method.Name)
                {
                    case "ConfigureMessageProcessor":
                        _callback = (Func<ProcessMessageEventArgs, ServiceBusReceivedMessage, CancellationToken, Task>)args[0]!;
                        return null;
                    case "StartAsync": return Task.CompletedTask;
                    case "ShutdownAsync": ShutdownCalls++; ShutdownEntered.TrySetResult(); return ShutdownRelease.Task;
                    case "CloseAsync": CloseCalls++; CloseEntered.TrySetResult(); return CloseRelease.Task;
                    case "get_InputAddress": return InputAddress;
                    default: throw new NotSupportedException(method.Name);
                }
            });
        }
        public Exception Failure { get; }
        public Uri InputAddress { get; } = new("sb://receiver-ownership.servicebus.invalid/owned-input");
        public ServiceBusReceivedMessage Message { get; }
        public ServiceBusReceiveEndpointContext Endpoint { get; }
        public ClientContext Client { get; }
        public RecordingReceiver Sdk { get; }
        public ServiceBusReceiveContext? Context { get; private set; }
        public ReceiveLockContext? Lock { get; private set; }
        public CancellationToken DeliveryToken { get; private set; }
        public CancellationToken DispatchToken { get; private set; }
        public Task? ActualDispatch { get; private set; }
        public int ActiveCount { get; private set; }
        public bool ExpectStopIdleRead { get; set; }
        public int ShutdownCalls { get; private set; }
        public int CloseCalls { get; private set; }
        public TaskCompletionSource DispatchEntered { get; } = NewSignal();
        public TaskCompletionSource DispatchRelease { get; } = NewSignal();
        public TaskCompletionSource ShutdownEntered { get; } = NewSignal();
        public TaskCompletionSource ShutdownRelease { get; } = NewSignal();
        public TaskCompletionSource CloseEntered { get; } = NewSignal();
        public TaskCompletionSource CloseRelease { get; } = NewSignal();
        public TaskCompletionSource StopIdleRead { get; } = NewSignal();
        public Task StartCallbackAsync(CancellationToken caller) => (_callback ?? throw new InvalidOperationException("Receiver.Start did not register callback"))(
            new ProcessMessageEventArgs(Message, Sdk, caller), Message, caller);
        public ServiceBusReceiveContext AssertActualContext()
        {
            ServiceBusReceiveContext context = Assert.IsType<ServiceBusReceiveContext>(Context);
            Assert.Equal(InputAddress, context.InputAddress);
            Assert.Equal("receiver-owned-message", context.MessageId);
            Assert.Equal(913L, context.SequenceNumber);
            Assert.True(context.Redelivered);
            Assert.Equal(new byte[] { 5, 10, 17 }, context.Body.ToArray());
            return context;
        }
        private object? DispatchMember(MethodInfo method, object?[] args)
        {
            switch (method.Name)
            {
                case "add_ZeroActivity": _zeroActivity += (ZeroActivityHandler)args[0]!; return null;
                case "remove_ZeroActivity": _zeroActivity -= (ZeroActivityHandler)args[0]!; return null;
                case "get_ActiveDispatchCount":
                    if (ExpectStopIdleRead) StopIdleRead.TrySetResult();
                    return ActiveCount;
                case "get_DispatchCount": return 1L;
                case "get_MaxConcurrentDispatchCount": return 1;
                case "DispatchAsync":
                    Context = Assert.IsType<ServiceBusReceiveContext>(args[0]);
                    Lock = Assert.IsAssignableFrom<ReceiveLockContext>(args[1]);
                    DeliveryToken = Context.CancellationToken;
                    DispatchToken = Assert.IsType<CancellationToken>(args[2]);
                    ActiveCount++;
                    ActualDispatch = PumpAsync(Lock);
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
                ActiveCount--;
                if (_zeroActivity is not null)
                    foreach (ZeroActivityHandler handler in _zeroActivity.GetInvocationList())
                        await handler().ConfigureAwait(false);
            }
        }
        public async Task DrainAsync(Task? pending, Receiver? receiver, Task? stop)
        {
            DispatchRelease.TrySetResult();
            Sdk.CompleteRelease.TrySetResult();
            ShutdownRelease.TrySetResult();
            CloseRelease.TrySetResult();
            try
            {
                try
                {
                    await ObserveAsync(pending ?? Task.CompletedTask);
                }
                finally
                {
                    try
                    {
                        if (ActualDispatch is not null) await ObserveAsync(ActualDispatch);
                    }
                    finally
                    {
                        // Join the manually managed consume lifecycle even if dispatch observation fails.
                        if (receiver is not null)
                        {
                            try
                            {
                                await (stop ?? receiver.StopAsync(CancellationToken.None)).WaitAsync(Timeout, CancellationToken.None);
                            }
                            finally
                            {
                                await ObserveAsync(receiver.Completed);
                            }
                        }
                    }
                }
            }
            finally
            {
                await Sdk.DisposeAsync();
            }
        }
        private async Task ObserveAsync(Task task)
        {
            try { await task.WaitAsync(Timeout, CancellationToken.None); }
            catch (Exception) when (task.IsFaulted && task.Exception!.Flatten().InnerExceptions.All(error => ReferenceEquals(error, Failure))) { }
            catch (OperationCanceledException) when (task.IsCanceled) { }
        }
        private static bool NoPayload(object?[] args) { args[0] = null; return false; }
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
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler(targetMethod ?? throw new InvalidOperationException("Missing proxy method"), args ?? []);
    }
    private sealed class RecordingReceiver : ServiceBusReceiver
    {
        public int CompleteCalls { get; private set; }
        public int DisposeCalls { get; private set; }
        public ServiceBusReceivedMessage? Message { get; private set; }
        public CancellationToken Token { get; private set; }
        public TaskCompletionSource CompleteEntered { get; } = NewSignal();
        public TaskCompletionSource CompleteRelease { get; } = NewSignal();
        public override Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
        {
            CompleteCalls++; Message = message; Token = cancellationToken; CompleteEntered.TrySetResult(); return CompleteRelease.Task;
        }
        public override ValueTask DisposeAsync() { DisposeCalls++; return ValueTask.CompletedTask; }
    }
}
