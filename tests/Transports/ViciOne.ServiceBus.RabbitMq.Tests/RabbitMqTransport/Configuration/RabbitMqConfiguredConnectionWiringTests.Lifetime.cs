using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Configuration;

public sealed partial class RabbitMqConfiguredConnectionWiringTests
{
    [Theory]
    [InlineData(0)] // pre-next IO
    [InlineData(1)] // pre-next owned cancellation
    [InlineData(2)] // no continuation
    [InlineData(3)] // rejected late continuation
    [InlineData(4)] // rejected duplicate while first creation is held
    [InlineData(5)] // custom filter returns without awaiting admitted next
    [InlineData(6)] // pipeline failure after admitted next
    [InlineData(7)] // explicitly delivered replacement connection
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONFIGURED-CONNECTION-WIRING",
        "public-negative-continuation-and-borrowed-channel-lifetime")]
    public async Task ConfigureConnection_PublicCreationNegativeAndLeaseBoundariesAsync(int mode)
    {
        ILogContext? previous = LogContext.Current;
        using var bound = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        bound.CancelAfter(TimeSpan.FromSeconds(10));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var primary = new IOException("Unique public connection pipeline failure");
        var canceled = new OperationCanceledException("Unique pre-next connection cancellation", cancellation.Token);
        var filter = new BoundaryConnectionFilter(mode, primary, canceled);
        PublicConnectionFixture? fixture = null;
        Task? operation = null;
        Task? stop = null;
        Exception? operationFailure = null;
        bool operationObserved = false;
        bool parentObserved = false;
        try
        {
            LogContext.ConfigureCurrentLogContext(NullLogger.Instance);
            fixture = new PublicConnectionFixture(filter);
            filter.Replacement = fixture.Replacement;
            RabbitMqReceiveEndpointContext runtime = Assert.IsAssignableFrom<RabbitMqReceiveEndpointContext>(fixture.Endpoint.CreateReceiveEndpointContext());
            fixture.Channels = runtime.ChannelContextSupervisor;
            var delivery = new HeldPublicChannelPipe();
            fixture.Delivery = delivery;
            operation = fixture.Channels.SendAsync(delivery, TestContext.Current.CancellationToken);

            await Task.WhenAny(filter.Entered.Task, fixture.Connection.CreateEntered.Task, operation).WaitAsync(bound.Token);
            Assert.Equal(1, filter.Calls);
            Assert.Equal(1, fixture.Supervisor.SendCalls);
            if (mode <= 3)
            {
                Exception? failure = await Record.ExceptionAsync(() => operation.WaitAsync(bound.Token));
                operationObserved = operation.IsCompleted;
                Assert.NotNull(failure);
                if (mode == 0)
                    Assert.Same(primary, failure);
                else if (mode == 1)
                {
                    OperationCanceledException observed = Assert.IsAssignableFrom<OperationCanceledException>(failure);
                    Assert.Equal(cancellation.Token, observed.CancellationToken);
                }
                else
                    Assert.Contains("did not invoke its continuation", Assert.IsType<InvalidOperationException>(failure).Message);
                Assert.Equal(0, fixture.Connection.CreateCalls);
                Assert.Equal(0, fixture.Replacement.CreateCalls);
                Assert.Equal(0, delivery.Calls);
                await Record.ExceptionAsync(() => fixture.Supervisor.SendTasks[0].WaitAsync(bound.Token));
                parentObserved = fixture.Supervisor.SendTasks[0].IsCompleted;
                if (mode == 3)
                {
                    Exception? late = await Record.ExceptionAsync(filter.InvokeLateAsync);
                    Assert.Contains("after completing", Assert.IsType<InvalidOperationException>(late).Message);
                    Assert.Equal(0, fixture.Connection.CreateCalls);
                    Assert.Equal(0, delivery.Calls);
                }
            }
            else
            {
                RecordingConnectionContext selected = mode == 7 ? fixture.Replacement : fixture.Connection;
                await selected.CreateEntered.Task.WaitAsync(bound.Token);
                await filter.Returned.Task.WaitAsync(bound.Token);
                Assert.Equal(1, selected.CreateCalls);
                Assert.Equal(mode == 7 ? 0 : 1, fixture.Connection.CreateCalls);
                Assert.Equal(mode == 7 ? 1 : 0, fixture.Replacement.CreateCalls);
                Assert.True(selected.CreateToken.CanBeCanceled);
                Assert.False(selected.CreationTask!.IsCompleted);
                Assert.False(fixture.Supervisor.SendTasks[0].IsCompleted);
                Assert.False(operation.IsCompleted);
                Assert.Equal(0, fixture.Connection.DisposeCalls);
                if (mode == 4)
                    Assert.Contains("more than once", Assert.IsType<InvalidOperationException>(filter.DuplicateFailure).Message);

                selected.CreateRelease.TrySetResult();
                await delivery.Entered.Task.WaitAsync(bound.Token);
                Assert.Equal(1, delivery.Calls);
                Assert.Same(selected, delivery.Context!.ConnectionContext);
                Assert.False(fixture.Supervisor.SendTasks[0].IsCompleted);
                delivery.Release.TrySetResult();
                await operation.WaitAsync(bound.Token);
                operationObserved = operation.IsCompleted;
                Assert.Equal(0, selected.ChannelDisposal.DisposeCalls);

                stop = fixture.Channels.StopAsync("public channel lease drain", CancellationToken.None);
                await selected.ChannelDisposal.Entered.Task.WaitAsync(bound.Token);
                Assert.False(stop.IsCompleted);
                Assert.False(fixture.Supervisor.SendTasks[0].IsCompleted);
                Assert.Equal(1, selected.ChannelDisposal.DisposeCalls);
                selected.ChannelDisposal.Release.TrySetResult();
                await stop.WaitAsync(bound.Token);
                await fixture.Channels.Completed.WaitAsync(bound.Token);
                await selected.CreationAgent!.Completed.WaitAsync(bound.Token);
                Exception? parentFailure = await Record.ExceptionAsync(() => fixture.Supervisor.SendTasks[0].WaitAsync(bound.Token));
                parentObserved = fixture.Supervisor.SendTasks[0].IsCompleted;
                if (mode == 6)
                    Assert.Same(primary, parentFailure);
                else
                    Assert.Null(parentFailure);
                Assert.Equal(1, selected.CreateCalls);
                Assert.Equal(1, selected.ChannelDisposal.DisposeCalls);
                Assert.Equal(0, fixture.Replacement.DisposeCalls);
            }
        }
        catch (Exception failure)
        {
            operationFailure = failure;
            throw;
        }
        finally
        {
            var failures = new List<Exception>();
            fixture?.ReleaseAll();
            async Task ObserveAsync(Func<Task> action)
            {
                using var fresh = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                try { await action().WaitAsync(fresh.Token); }
                catch (Exception failure) { failures.Add(failure); }
            }
            try
            {
                if (fixture?.Channels is { } channels)
                {
                    await ObserveAsync(() => stop ?? channels.StopAsync("fixture final channel drain", CancellationToken.None));
                    await ObserveAsync(() => channels.Completed);
                }
                if (operation is not null && !operationObserved)
                    await ObserveAsync(() => operation);
                if (fixture is not null)
                {
                    await ObserveAsync(() => fixture.Supervisor.StopAsync("fixture parent owner drain", CancellationToken.None));
                    await ObserveAsync(() => fixture.Supervisor.Completed);
                    if (!parentObserved)
                        foreach (Task owned in fixture.Supervisor.SendTasks)
                            await ObserveAsync(() => owned);
                    foreach (RecordingConnectionContext context in new[] { fixture.Connection, fixture.Replacement })
                    {
                        if (context.CreationTask is { } creation)
                            await ObserveAsync(() => creation);
                        if (context.CreationAgent is { } agent)
                            await ObserveAsync(() => agent.Completed);
                    }
                    if (filter.Admitted is { } admitted)
                        await ObserveAsync(() => admitted);
                    await ObserveAsync(() => fixture.Replacement.DisposeAsync().AsTask());
                    await ObserveAsync(() =>
                    {
                        Assert.Equal(1, fixture.Connection.DisposeCalls);
                        Assert.Equal(1, fixture.Replacement.DisposeCalls);
                        return Task.CompletedTask;
                    });
                }
            }
            finally { LogContext.Current = previous; }
            if (failures.Count > 0)
            {
                if (operationFailure is not null) failures.Insert(0, operationFailure);
                if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
                throw new AggregateException("Public connection test or owned cleanup failed.", failures);
            }
        }
    }

    private sealed class BoundaryConnectionFilter(int mode, Exception primary, OperationCanceledException canceled)
        : IFilter<ConnectionContext>
    {
        private int _calls;
        private IPipe<ConnectionContext>? _next;
        private ConnectionContext? _context;
        public int Calls => Volatile.Read(ref _calls);
        public RecordingConnectionContext Replacement { get; set; } = null!;
        public TaskCompletionSource Entered { get; } = Signal();
        public TaskCompletionSource Returned { get; } = Signal();
        public Exception? DuplicateFailure { get; private set; }
        public Task? Admitted { get; private set; }

        public async Task SendAsync(ConnectionContext context, IPipe<ConnectionContext> next)
        {
            Interlocked.Increment(ref _calls);
            _next = next;
            _context = context;
            Entered.TrySetResult();
            try
            {
                if (mode == 0) throw primary;
                if (mode == 1) throw canceled;
                if (mode <= 3) return;
                Admitted = next.SendAsync(mode == 7 ? Replacement : context);
                if (mode == 4)
                {
                    try { await next.SendAsync(context); }
                    catch (Exception failure) { DuplicateFailure = failure; }
                }
                if (mode == 6) throw primary;
            }
            finally { Returned.TrySetResult(); }
        }

        public Task InvokeLateAsync() => _next!.SendAsync(_context!);
        public void Probe(ProbeContext context) => context.CreateFilterScope("publicConnectionBoundary");
    }

    private sealed class HeldPublicChannelPipe : IPipe<ChannelContext>
    {
        private int _calls;
        public int Calls => Volatile.Read(ref _calls);
        public ChannelContext? Context { get; private set; }
        public TaskCompletionSource Entered { get; } = Signal();
        public TaskCompletionSource Release { get; } = Signal();
        public async Task SendAsync(ChannelContext context)
        {
            Context = context;
            Interlocked.Increment(ref _calls);
            Entered.TrySetResult();
            await Release.Task;
        }
        public void Probe(ProbeContext context) => context.CreateFilterScope("publicHeldChannelLease");
    }

    private sealed class PublicConnectionFixture
    {
        public PublicConnectionFixture(IFilter<ConnectionContext> filter)
        {
            var bus = new RabbitMqBusConfiguration(new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology()));
            IRabbitMqHostConfiguration actualHost = bus.HostConfiguration;
            actualHost.LogContext = LogContext.Current;
            Connection = new RecordingConnectionContext(actualHost);
            Replacement = new RecordingConnectionContext(actualHost);
            Supervisor = new RecordingBorrowedConnectionSupervisor(new ConnectionFixtureFactory(Connection, actualHost));
            IRabbitMqHostConfiguration host = DispatchProxy.Create<IRabbitMqHostConfiguration, HostConfigurationProxy>();
            var proxy = (HostConfigurationProxy)(object)host;
            proxy.Inner = actualHost;
            proxy.Supervisor = Supervisor;
            IRabbitMqEndpointConfiguration child = bus.CreateEndpointConfiguration(false);
            child.AutoStart = false;
            var settings = new RabbitMqReceiveSettings(child, "public-connection-boundary", ExchangeType.Fanout, true, false);
            Endpoint = new RabbitMqReceiveEndpointConfiguration(host, settings, child);
            Endpoint.ConfigureConnection(c => c.UseFilter(filter));
        }
        public RecordingConnectionContext Connection { get; }
        public RecordingConnectionContext Replacement { get; }
        public RecordingBorrowedConnectionSupervisor Supervisor { get; }
        public RabbitMqReceiveEndpointConfiguration Endpoint { get; }
        public IChannelContextSupervisor? Channels { get; set; }
        public HeldPublicChannelPipe? Delivery { get; set; }
        public void ReleaseAll()
        {
            Connection.CreateRelease.TrySetResult();
            Replacement.CreateRelease.TrySetResult();
            Connection.ChannelDisposal.Release.TrySetResult();
            Replacement.ChannelDisposal.Release.TrySetResult();
            Delivery?.Release.TrySetResult();
        }
    }

    private sealed class RecordingBorrowedConnectionSupervisor(IPipeContextFactory<ConnectionContext> factory)
        : TransportPipeContextSupervisor<ConnectionContext>(factory), IConnectionContextSupervisor
    {
        private readonly List<Task> _sendTasks = [];
        public int SendCalls { get { lock (_sendTasks) return _sendTasks.Count; } }
        public Task[] SendTasks { get { lock (_sendTasks) return _sendTasks.ToArray(); } }
        public new Task SendAsync(IPipe<ConnectionContext> pipe, CancellationToken cancellationToken = default)
        {
            Task operation = base.SendAsync(pipe, cancellationToken);
            lock (_sendTasks) _sendTasks.Add(operation);
            return operation;
        }
        public Uri NormalizeAddress(Uri address) => throw Unexpected(nameof(NormalizeAddress));
        public Task<ISendTransport> CreateSendTransportAsync(RabbitMqReceiveEndpointContext context,
            IChannelContextSupervisor channels, Uri address, CancellationToken cancellationToken = default)
            => throw Unexpected(nameof(CreateSendTransportAsync));
        public Task<ISendTransport> CreatePublishTransportAsync<T>(RabbitMqReceiveEndpointContext context,
            IChannelContextSupervisor channels, CancellationToken cancellationToken = default) where T : class
            => throw Unexpected(nameof(CreatePublishTransportAsync));
    }
}
