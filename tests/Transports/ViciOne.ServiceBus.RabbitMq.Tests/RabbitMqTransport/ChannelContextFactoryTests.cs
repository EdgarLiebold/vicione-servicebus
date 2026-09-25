using System.Reflection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class ChannelContextFactoryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CHANNEL-LIFECYCLE", "active-lease-borrows-channel-and-links-cancellation")]
    public async Task CreateActiveContext_BorrowsTheChannelAndLinksBothCancellationSourcesAsync()
    {
        using var ownerCancellation = new CancellationTokenSource();
        using var callerCancellation = new CancellationTokenSource();
        IChannel channel = CreateChannel();
        ChannelContext ownerContext = CreateContext(channel, ownerCancellation.Token);
        var owner = new TestContextHandle(Task.FromResult(ownerContext));
        var supervisor = new TestSupervisor();
        var factory = new ChannelContextFactory(null!, null);

        IActivePipeContextAgent<ChannelContext> active = factory.CreateActiveContext(supervisor, owner, callerCancellation.Token);
        var lease = Assert.IsType<ScopeChannelContext>(await active.Context);

        Assert.Same(channel, lease.Channel);
        Assert.Equal(1, supervisor.TotalCount);
        Assert.False(lease.CancellationToken.IsCancellationRequested);
        callerCancellation.Cancel();
        Assert.True(lease.CancellationToken.IsCancellationRequested);
        await active.DisposeAsync();
        Assert.False(owner.IsDisposed);

        using var secondCaller = new CancellationTokenSource();
        IActivePipeContextAgent<ChannelContext> second = factory.CreateActiveContext(supervisor, owner, secondCaller.Token);
        var secondLease = Assert.IsType<ScopeChannelContext>(await second.Context);
        Assert.False(secondLease.CancellationToken.IsCancellationRequested);
        ownerCancellation.Cancel();
        Assert.True(secondLease.CancellationToken.IsCancellationRequested);
        await second.DisposeAsync();
        Assert.False(owner.IsDisposed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CHANNEL-LIFECYCLE", "released-lease-removes-owner-cancellation-registration")]
    public async Task CreateActiveContext_ReleasesTheBorrowedScopeAfterUseAsync()
    {
        using var ownerCancellation = new CancellationTokenSource();
        using var callerCancellation = new CancellationTokenSource();
        var owner = new TestContextHandle(Task.FromResult(CreateContext(CreateChannel(), ownerCancellation.Token)));
        var factory = new ChannelContextFactory(null!, null);

        IActivePipeContextAgent<ChannelContext> active = factory.CreateActiveContext(
            new TestSupervisor(), owner, callerCancellation.Token);
        var lease = Assert.IsType<ScopeChannelContext>(await active.Context);
        CancellationToken linkedToken = lease.CancellationToken;
        Assert.False(linkedToken.IsCancellationRequested);

        await active.StopAsync(TestContext.Current.CancellationToken);
        await active.DisposeAsync();
        ownerCancellation.Cancel();

        Assert.False(linkedToken.IsCancellationRequested);
        Assert.False(owner.IsDisposed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CHANNEL-LIFECYCLE", "concurrent-stop-keeps-inflight-owner-cancellation")]
    public async Task SendAsync_ConcurrentStopKeepsTheInFlightChannelCancellationLinkedAsync()
    {
        using var ownerCancellation = new CancellationTokenSource();
        var factory = new TestChannelFactory(CreateContext(CreateChannel(), ownerCancellation.Token));
        var supervisor = new PipeContextSupervisor<ChannelContext>(factory);
        var pipe = new BlockingPipe();

        Task send = supervisor.SendAsync(pipe, TestContext.Current.CancellationToken);
        var lease = Assert.IsType<ScopeChannelContext>(
            await pipe.Entered.Task.WaitAsync(TestContext.Current.CancellationToken));
        CancellationToken linkedToken = lease.CancellationToken;
        Assert.False(linkedToken.IsCancellationRequested);
        IActivePipeContextAgent<ChannelContext> active = Assert.IsAssignableFrom<IActivePipeContextAgent<ChannelContext>>(factory.Active);
        var stopping = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = active.Stopping.Register(() => stopping.TrySetResult());

        Task stop = supervisor.StopAsync("stop while the channel pipe is running", TestContext.Current.CancellationToken);
        await stopping.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.False(stop.IsCompleted);
        ownerCancellation.Cancel();
        Assert.True(linkedToken.IsCancellationRequested);

        pipe.Release.TrySetResult();
        await send;
        await stop;
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CHANNEL-LIFECYCLE", "bounded-stop-retries-after-active-use")]
    public async Task CreateActiveContext_StopBudgetCanExpireAndRetryAfterUseEndsAsync()
    {
        using var ownerCancellation = new CancellationTokenSource();
        using var stopBudget = new CancellationTokenSource();
        var owner = new TestContextHandle(Task.FromResult(CreateContext(CreateChannel(), ownerCancellation.Token)));
        IActivePipeContextAgent<ChannelContext> active = new ChannelContextFactory(null!, null)
            .CreateActiveContext(new TestSupervisor(), owner, TestContext.Current.CancellationToken);
        var lease = Assert.IsType<ScopeChannelContext>(await active.Context);
        CancellationToken linkedToken = lease.CancellationToken;
        using IDisposable use = Assert.IsType<ActivePipeContextAgent<ChannelContext>>(active).BeginUse();

        Task stop = active.StopAsync(new TestStopContext(stopBudget.Token), TestContext.Current.CancellationToken);
        Assert.False(stop.IsCompleted);
        stopBudget.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stop);
        Assert.False(active.Completed.IsCompleted);
        Assert.False(owner.IsDisposed);

        use.Dispose();
        await active.StopAsync(new TestStopContext(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);
        ownerCancellation.Cancel();
        Assert.False(linkedToken.IsCancellationRequested);
        Assert.True(active.Completed.IsCompletedSuccessfully);
        Assert.False(owner.IsDisposed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CHANNEL-LIFECYCLE", "late-channel-acquisition-releases-abandoned-scope")]
    public async Task CreateActiveContext_ReleasesAScopeCompletedAfterTheBorrowerEndsAsync()
    {
        using var ownerCancellation = new CancellationTokenSource();
        var pending = new TaskCompletionSource<ChannelContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = new TestContextHandle(pending.Task);
        IActivePipeContextAgent<ChannelContext> active = new ChannelContextFactory(null!, null)
            .CreateActiveContext(new TestSupervisor(), owner, TestContext.Current.CancellationToken);

        await active.DisposeAsync();
        pending.TrySetResult(CreateContext(CreateChannel(), ownerCancellation.Token));
        var lease = Assert.IsType<ScopeChannelContext>(await active.Context.WaitAsync(TestContext.Current.CancellationToken));
        using var releaseDeadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        releaseDeadline.CancelAfter(TimeSpan.FromSeconds(5));
        while (lease.CancellationToken != TestContext.Current.CancellationToken)
            await Task.Delay(1, releaseDeadline.Token);
        CancellationToken afterRelease = lease.CancellationToken;
        ownerCancellation.Cancel();

        Assert.False(afterRelease.IsCancellationRequested);
        Assert.False(owner.IsDisposed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CHANNEL-LIFECYCLE", "closed-channel-preserves-or-synthesizes-broker-reason")]
    public async Task CreateActiveContext_RejectsAClosedChannelWithTheBrokerReasonAsync(bool hasBrokerReason)
    {
        IChannel channel = CreateChannel();
        var proxy = (ChannelProxy)(object)channel;
        proxy.IsClosed = true;
        var brokerReason = new ShutdownEventArgs(ShutdownInitiator.Peer, 541, "internal error");
        proxy.CloseReason = hasBrokerReason ? brokerReason : null;
        var owner = new TestContextHandle(Task.FromResult(CreateContext(channel, TestContext.Current.CancellationToken)));
        var factory = new ChannelContextFactory(null!, null);

        IActivePipeContextAgent<ChannelContext> active = factory.CreateActiveContext(
            new TestSupervisor(), owner, TestContext.Current.CancellationToken);
        OperationInterruptedException actual = await Assert.ThrowsAsync<OperationInterruptedException>(() => active.Context);

        if (hasBrokerReason)
            Assert.Same(brokerReason, actual.ShutdownReason);
        else
        {
            var synthesized = Assert.IsType<ShutdownEventArgs>(actual.ShutdownReason);
            Assert.Equal(ShutdownInitiator.Library, synthesized.Initiator);
            Assert.Equal(491, synthesized.ReplyCode);
            Assert.Contains("no longer available", synthesized.ReplyText, StringComparison.OrdinalIgnoreCase);
        }
        Assert.False(owner.IsDisposed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CHANNEL-LIFECYCLE", "active-lease-wait-honors-caller-cancellation")]
    public async Task CreateActiveContext_CancelsAWaitWithoutDisposingTheChannelOwnerAsync()
    {
        var pending = new TaskCompletionSource<ChannelContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = new TestContextHandle(pending.Task);
        using var caller = new CancellationTokenSource();
        var factory = new ChannelContextFactory(null!, null);
        IActivePipeContextAgent<ChannelContext> active = factory.CreateActiveContext(new TestSupervisor(), owner, caller.Token);

        caller.Cancel();
        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => active.Context);

        Assert.Equal(caller.Token, actual.CancellationToken);
        Assert.False(owner.IsDisposed);
        Assert.False(pending.Task.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CHANNEL-LIFECYCLE", "channel-creation-failure-preserves-primary-cause")]
    public async Task CreateActiveContext_PreservesTheChannelCreationFailureAsync()
    {
        var expected = new OperationInterruptedException(
            new ShutdownEventArgs(ShutdownInitiator.Peer, 504, "channel creation failed"));
        var owner = new TestContextHandle(Task.FromException<ChannelContext>(expected));
        var factory = new ChannelContextFactory(null!, null);

        IActivePipeContextAgent<ChannelContext> active = factory.CreateActiveContext(
            new TestSupervisor(), owner, TestContext.Current.CancellationToken);
        OperationInterruptedException actual = await Assert.ThrowsAsync<OperationInterruptedException>(() => active.Context);

        Assert.Same(expected, actual);
        Assert.False(owner.IsDisposed);
    }

    private static IChannel CreateChannel() => DispatchProxy.Create<IChannel, ChannelProxy>();

    private static ChannelContext CreateContext(IChannel channel, CancellationToken cancellationToken = default)
    {
        ChannelContext context = DispatchProxy.Create<ChannelContext, ChannelContextProxy>();
        ((ChannelContextProxy)(object)context).Channel = channel;
        ((ChannelContextProxy)(object)context).CancellationToken = cancellationToken;
        return context;
    }

    private sealed class TestContextHandle(Task<ChannelContext> context) : IPipeContextHandle<ChannelContext>
    {
        public bool IsDisposed { get; private set; }
        public Task<ChannelContext> Context { get; } = context;

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return default;
        }
    }

    private sealed class TestStopContext(CancellationToken cancellationToken) : BasePipeContext(cancellationToken), StopContext
    {
        public string Reason => "bounded stop";
    }

    private sealed class TestChannelFactory(ChannelContext owner) : IPipeContextFactory<ChannelContext>
    {
        private readonly ChannelContextFactory _factory = new(null!, null);

        public IActivePipeContextAgent<ChannelContext>? Active { get; private set; }

        public IPipeContextAgent<ChannelContext> CreateContext(ISupervisor supervisor) => supervisor.AddContext(owner);

        public IActivePipeContextAgent<ChannelContext> CreateActiveContext(
            ISupervisor supervisor, IPipeContextHandle<ChannelContext> context, CancellationToken cancellationToken)
        {
            Active = _factory.CreateActiveContext(supervisor, context, cancellationToken);
            return Active;
        }
    }

    private sealed class BlockingPipe : IPipe<ChannelContext>
    {
        public TaskCompletionSource<ChannelContext> Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task SendAsync(ChannelContext context)
        {
            Entered.TrySetResult(context);
            await Release.Task.WaitAsync(TestContext.Current.CancellationToken);
        }

        public void Probe(ProbeContext context) { }
    }

    private sealed class TestSupervisor : ISupervisor
    {
        private long _totalCount;

        public Task Ready => Task.CompletedTask;
        public Task Completed => Task.CompletedTask;
        public CancellationToken Stopping => default;
        public CancellationToken Stopped => default;
        public int PeakActiveCount => _totalCount > 0 ? 1 : 0;
        public long TotalCount => _totalCount;

        public void Add(IAgent agent)
        {
            ArgumentNullException.ThrowIfNull(agent);
            Interlocked.Increment(ref _totalCount);
        }

        public Task StopAsync(StopContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private class ChannelProxy : DispatchProxy
    {
        public bool IsClosed { get; set; }
        public ShutdownEventArgs? CloseReason { get; set; }

        protected override object? Invoke(MethodInfo? method, object?[]? args) => method?.Name switch
        {
            "get_IsClosed" => IsClosed,
            "get_CloseReason" => CloseReason,
            _ => throw new NotSupportedException($"Unexpected broker operation: {method?.Name}"),
        };
    }

    private class ChannelContextProxy : DispatchProxy
    {
        public IChannel Channel { get; set; } = null!;
        public CancellationToken CancellationToken { get; set; }

        protected override object? Invoke(MethodInfo? method, object?[]? args) => method?.Name switch
        {
            "get_Channel" => Channel,
            "get_CancellationToken" => CancellationToken,
            _ => throw new NotSupportedException($"Unexpected channel context operation: {method?.Name}"),
        };
    }
}
