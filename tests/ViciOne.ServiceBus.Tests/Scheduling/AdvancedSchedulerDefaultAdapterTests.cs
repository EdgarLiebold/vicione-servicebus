using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Topology;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Scheduling;

public sealed class AdvancedSchedulerDefaultAdapterTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-ADVANCED-SCHEDULER-DEFAULT", "relative-default-real-clock-token-and-cancel-admission")]
    public async Task RelativeDefaultAdapters_UseActualClockAndCallerTokenAndSkipCollaboratorsWhenPreCanceledAsync(int route)
    {
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var clock = new CountingClock();
        var provider = new RecordingProvider();
        IBusTopology topology = DispatchProxy.Create<IBusTopology, PublishTopologyProxy>();
        var topologyRecorder = (PublishTopologyProxy)(object)topology;
        IMessageScheduler scheduler = new MessageScheduler(provider, topology, clock);
        var message = new Payload("scheduled");
        TimeSpan delay = TimeSpan.FromMinutes(7);
        var options = new ScheduleOptions
        {
            Headers = new Dictionary<string, object?> { ["default-route"] = "options" },
            TimeToLive = TimeSpan.FromMinutes(11),
        };

        ScheduledMessage<Payload> result = await InvokeRouteAsync(scheduler, route, delay, message, options, caller.Token)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        Uri destination = route == 2 ? PublishTopologyProxy.PublishAddress : Destination;
        Assert.Equal(1, clock.Reads);
        Assert.Equal(1, provider.ScheduleCalls);
        Assert.Equal(route == 2 ? 1 : 0, topologyRecorder.Calls);
        if (route == 2)
            Assert.Equal(typeof(Payload), topologyRecorder.Contract);
        Assert.Equal(destination, provider.Destination);
        Assert.Equal(clock.UtcNow + delay, provider.DueAt);
        Assert.Same(message, provider.Message);
        Assert.Equal(caller.Token, provider.CallerToken);
        Assert.Equal(provider.TokenId, result.TokenId);
        Assert.Equal(provider.DueAt, result.DueAt);
        Assert.Equal(destination, result.Destination);
        Assert.Same(message, result.Payload);
        if (route == 1)
        {
            var context = Assert.IsType<InMemorySendContext<Payload>>(provider.Context);
            Assert.Equal("options", context.Headers.Get<string>("default-route"));
            Assert.Equal(options.TimeToLive, context.TimeToLive);
        }

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var forbiddenClock = new CountingClock { ThrowOnRead = true };
        var untouchedProvider = new RecordingProvider();
        IBusTopology untouchedTopology = DispatchProxy.Create<IBusTopology, PublishTopologyProxy>();
        IMessageScheduler canceledScheduler = new MessageScheduler(untouchedProvider, untouchedTopology, forbiddenClock);
        Task<ScheduledMessage<Payload>> canceledOperation = InvokeRouteAsync(canceledScheduler, route, delay, message, options, canceled.Token);
        Assert.True(canceledOperation.IsCanceled);
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledOperation);
        Assert.Equal(canceled.Token, exception.CancellationToken);
        Assert.Equal(0, forbiddenClock.Reads);
        Assert.Equal(0, untouchedProvider.ScheduleCalls);
        Assert.Equal(0, ((PublishTopologyProxy)(object)untouchedTopology).Calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-SCHEDULER-DEFAULT", "descriptor-cancellation-provider-task-identity-and-guards")]
    public async Task DescriptorCancellation_PreservesProviderTaskIdentityAndArgumentsAsync()
    {
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new RecordingProvider { CancellationTask = gate.Task };
        var clock = new CountingClock { ThrowOnRead = true };
        IBusTopology topology = DispatchProxy.Create<IBusTopology, PublishTopologyProxy>();
        IMessageScheduler scheduler = new MessageScheduler(provider, topology, clock);
        var descriptor = new ScheduledMessageHandle<Payload>(provider.TokenId, clock.UtcNow, Destination, new Payload("cancel"));
        Task? operation = null;
        try
        {
            operation = scheduler.CancelScheduledSendAsync(descriptor, caller.Token);
            Assert.Same(gate.Task, operation);
            Assert.False(operation.IsCompleted);
            Assert.Equal(1, provider.CancelCalls);
            Assert.Same(descriptor.Destination, provider.Destination);
            Assert.Equal(descriptor.TokenId, provider.CanceledTokenId);
            Assert.Equal(caller.Token, provider.CallerToken);
            Assert.Equal(0, clock.Reads);
            Assert.Equal(0, ((PublishTopologyProxy)(object)topology).Calls);
            gate.SetResult();
            await operation.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.True(operation.IsCompletedSuccessfully);

            Exception? nullFailure = Record.Exception(() =>
            {
                _ = scheduler.CancelScheduledSendAsync((ScheduledMessage)null!, caller.Token);
            });
            Assert.Equal("scheduled", Assert.IsType<ArgumentNullException>(nullFailure).ParamName);
            Assert.Equal(1, provider.CancelCalls);

            var primary = new IOException("descriptor provider synchronous failure");
            provider.CancellationFailure = primary;
            Exception? observed = Record.Exception(() =>
            {
                _ = scheduler.CancelScheduledSendAsync(descriptor, caller.Token);
            });
            Assert.Same(primary, Assert.IsType<IOException>(observed));
            Assert.Equal(2, provider.CancelCalls);
        }
        finally
        {
            gate.TrySetResult();
            if (operation is not null)
                await operation.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        }
    }

    private static readonly Uri Destination = new("loopback://localhost/default-scheduler");

    private static Task<ScheduledMessage<Payload>> InvokeRouteAsync(IMessageScheduler scheduler, int route, TimeSpan delay,
        Payload message, ScheduleOptions options, CancellationToken cancellationToken) => route switch
    {
        0 => scheduler.ScheduleSendAsync(Destination, delay, message, cancellationToken),
        1 => scheduler.ScheduleSendAsync(Destination, delay, message, options, cancellationToken),
        2 => scheduler.SchedulePublishAsync(delay, message, cancellationToken),
        _ => throw new ArgumentOutOfRangeException(nameof(route)),
    };

    private sealed record Payload(string Value);

    private sealed class CountingClock : TimeProvider
    {
        public DateTimeOffset UtcNow { get; } = new(2044, 6, 7, 8, 9, 10, TimeSpan.Zero);
        public bool ThrowOnRead { get; init; }
        public int Reads { get; private set; }
        public override DateTimeOffset GetUtcNow()
        {
            Reads++;
            if (ThrowOnRead)
                throw new InvalidOperationException("A canceled default adapter must not read its clock.");
            return UtcNow;
        }
    }

    private sealed class RecordingProvider : IScheduleMessageProvider
    {
        public Guid TokenId { get; } = Guid.Parse("77000000-0000-0000-0000-000000000077");
        public int ScheduleCalls { get; private set; }
        public int CancelCalls { get; private set; }
        public Uri? Destination { get; private set; }
        public DateTimeOffset DueAt { get; private set; }
        public object? Message { get; private set; }
        public SendContext? Context { get; private set; }
        public CancellationToken CallerToken { get; private set; }
        public Guid CanceledTokenId { get; private set; }
        public Task CancellationTask { get; init; } = Task.CompletedTask;
        public Exception? CancellationFailure { get; set; }

        public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message,
            IPipe<SendContext<T>> pipe, CancellationToken cancellationToken) where T : class
        {
            ScheduleCalls++;
            Destination = destinationAddress;
            DueAt = dueAt;
            Message = message;
            CallerToken = cancellationToken;
            var context = new InMemorySendContext<T>(message, cancellationToken);
            Context = context;
            await pipe.SendAsync(context);
            return new ScheduledMessageHandle<T>(TokenId, dueAt, destinationAddress, message);
        }

        public Task CancelScheduledSendAsync(Guid tokenId, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The descriptor must forward its destination.");

        public Task CancelScheduledSendAsync(Uri destinationAddress, Guid tokenId, CancellationToken cancellationToken)
        {
            CancelCalls++;
            Destination = destinationAddress;
            CanceledTokenId = tokenId;
            CallerToken = cancellationToken;
            if (CancellationFailure is not null)
                throw CancellationFailure;
            return CancellationTask;
        }
    }

    private class PublishTopologyProxy : DispatchProxy
    {
        public static readonly Uri PublishAddress = new("loopback://localhost/published-default");
        public int Calls { get; private set; }
        public Type? Contract { get; private set; }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is null || targetMethod.Name != nameof(IBusTopology.TryGetPublishAddress) || !targetMethod.IsGenericMethod
                || args is null || args.Length != 1)
                throw new InvalidOperationException($"Unexpected topology boundary: {targetMethod?.Name}.");
            Calls++;
            Contract = targetMethod.GetGenericArguments()[0];
            args[0] = PublishAddress;
            return true;
        }
    }
}
