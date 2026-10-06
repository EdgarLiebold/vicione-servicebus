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
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Configuration;

public sealed partial class RabbitMqConfiguredConnectionWiringTests
{
    [Theory]
    [InlineData(false)] // two endpoint continuations sharing one borrowed parent
    [InlineData(true)] // one compiled endpoint, two independently owned generations
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONFIGURED-CONNECTION-WIRING",
        "compiled-specs-parallel-endpoints-and-current-host-recycle")]
    public async Task ConfigureConnection_CompiledSpecsStayLocalAcrossEndpointsAndRecyclingAsync(bool recycle)
    {
        ILogContext? previous = LogContext.Current;
        using var bound = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        bound.CancelAfter(TimeSpan.FromSeconds(10));
        var roots = new List<RecordingConnectionContext>();
        var borrowedReplacements = new List<RecordingConnectionContext>();
        var parents = new List<GenerationBorrowedConnectionSupervisor>();
        var runs = new List<PublicGenerationRun>();
        var filters = new List<BoundaryConnectionFilter>();
        Exception? operationFailure = null;
        try
        {
            LogContext.ConfigureCurrentLogContext(NullLogger.Instance);
            var bus = new RabbitMqBusConfiguration(new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology()));
            IRabbitMqHostConfiguration actualHost = bus.HostConfiguration;
            actualHost.LogContext = LogContext.Current;
            var root = new RecordingConnectionContext(actualHost);
            roots.Add(root);
            var parent = new GenerationBorrowedConnectionSupervisor(new ConnectionFixtureFactory(root, actualHost));
            parents.Add(parent);
            IRabbitMqHostConfiguration host = DispatchProxy.Create<IRabbitMqHostConfiguration, HostConfigurationProxy>();
            var hostProxy = (HostConfigurationProxy)(object)host;
            hostProxy.Inner = actualHost;
            hostProxy.Supervisor = parent;
            var filter1 = new BoundaryConnectionFilter(recycle ? 5 : 7,
                new InvalidOperationException("Unexpected first generation failure"),
                new OperationCanceledException("Unexpected first generation cancellation"));
            RecordingConnectionContext first = root;
            if (!recycle)
            {
                first = new RecordingConnectionContext(actualHost);
                borrowedReplacements.Add(first);
                filter1.Replacement = first;
            }
            filters.Add(filter1);
            var spec1 = new CountingConnectionSpecification(filter1);
            RabbitMqReceiveEndpointContext endpoint1 = CreatePublicGenerationEndpoint(bus, host, "connection-generation-one", spec1);
            Assert.Equal(1, spec1.ApplyCalls);
            Assert.Equal(0, filter1.Calls);
            Assert.Equal(0, parent.SendCalls);
            var run1 = new PublicGenerationRun(endpoint1.ChannelContextSupervisor, parent, first);
            runs.Add(run1);
            await first.CreateEntered.Task.WaitAsync(bound.Token);
            run1.ParentOperation = await run1.ParentPublished.WaitAsync(bound.Token);
            Assert.Equal(1, filter1.Calls);
            Assert.Equal(1, first.CreateCalls);
            Assert.False(run1.ParentOperation.IsCompleted);

            PublicGenerationRun run2;
            CountingConnectionSpecification? spec2 = null;
            BoundaryConnectionFilter? filter2 = null;
            if (!recycle)
            {
                var second = new RecordingConnectionContext(actualHost);
                borrowedReplacements.Add(second);
                filter2 = new BoundaryConnectionFilter(7,
                    new InvalidOperationException("Unexpected second endpoint failure"),
                    new OperationCanceledException("Unexpected second endpoint cancellation")) { Replacement = second };
                filters.Add(filter2);
                spec2 = new CountingConnectionSpecification(filter2);
                RabbitMqReceiveEndpointContext endpoint2 = CreatePublicGenerationEndpoint(bus, host, "connection-generation-two", spec2);
                run2 = new PublicGenerationRun(endpoint2.ChannelContextSupervisor, parent, second);
                runs.Add(run2);
                await second.CreateEntered.Task.WaitAsync(bound.Token);
                run2.ParentOperation = await run2.ParentPublished.WaitAsync(bound.Token);
                Assert.NotSame(run1.Channels, run2.Channels);
                Assert.Equal(1, spec2.ApplyCalls);
                Assert.Equal(1, filter2.Calls);
                Assert.Equal(2, parent.SendCalls);
                Assert.Equal(0, root.CreateCalls);
                Assert.False(run2.Operation.IsCompleted);
                Assert.False(run2.ParentOperation.IsCompleted);
                await CompleteAndStopGenerationAsync(run1, bound.Token);
                Assert.False(run2.Operation.IsCompleted);
                Assert.False(run2.ParentOperation.IsCompleted);
                Assert.Equal(0, second.ChannelDisposal.DisposeCalls);
                Assert.Equal(0, root.DisposeCalls);
                await CompleteAndStopGenerationAsync(run2, bound.Token);
                Assert.Equal(1, filter1.Calls);
                Assert.Equal(1, filter2.Calls);
                Assert.Equal(1, spec1.ApplyCalls);
                Assert.Equal(1, spec2.ApplyCalls);
                Assert.Equal(0, first.DisposeCalls);
                Assert.Equal(0, second.DisposeCalls);
            }
            else
            {
                await CompleteAndStopGenerationAsync(run1, bound.Token);
                Assert.Equal(0, root.DisposeCalls);
                var secondRoot = new RecordingConnectionContext(actualHost);
                roots.Add(secondRoot);
                var secondParent = new GenerationBorrowedConnectionSupervisor(new ConnectionFixtureFactory(secondRoot, actualHost));
                parents.Add(secondParent);
                hostProxy.Supervisor = secondParent;
                IChannelContextSupervisor nextChannels = endpoint1.ChannelContextSupervisor;
                Assert.NotSame(run1.Channels, nextChannels);
                run2 = new PublicGenerationRun(nextChannels, secondParent, secondRoot);
                runs.Add(run2);
                await secondRoot.CreateEntered.Task.WaitAsync(bound.Token);
                run2.ParentOperation = await run2.ParentPublished.WaitAsync(bound.Token);
                Assert.Equal(2, filter1.Calls);
                Assert.Equal(1, spec1.ApplyCalls);
                Assert.Equal(1, parent.SendCalls);
                Assert.Equal(1, secondParent.SendCalls);
                Assert.Equal(1, root.CreateCalls);
                Assert.Equal(1, secondRoot.CreateCalls);
                Assert.False(run2.ParentOperation.IsCompleted);
                Assert.Equal(0, secondRoot.ChannelDisposal.DisposeCalls);
                Assert.Equal(0, secondRoot.DisposeCalls);
                await CompleteAndStopGenerationAsync(run2, bound.Token);
                Assert.Equal(1, spec1.ApplyCalls);
                Assert.Equal(2, filter1.Calls);
            }
            Assert.Equal(2, hostProxy.SupervisorReads);
            foreach (PublicGenerationRun run in runs)
            {
                Assert.Equal(1, run.Raw.CreateCalls);
                Assert.Equal(1, run.Raw.ChannelDisposal.DisposeCalls);
                Assert.True(run.ParentOperation.IsCompletedSuccessfully);
                Assert.True(run.Operation.IsCompletedSuccessfully);
            }
        }
        catch (Exception failure)
        {
            operationFailure = failure;
            throw;
        }
        finally
        {
            foreach (PublicGenerationRun run in runs) run.ReleaseAll();
            foreach (RecordingConnectionContext context in roots.Concat(borrowedReplacements))
            {
                context.CreateRelease.TrySetResult();
                context.ChannelDisposal.Release.TrySetResult();
            }
            var failures = new List<Exception>();
            async Task ObserveAsync(Func<Task> action)
            {
                using var fresh = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                try { await action().WaitAsync(fresh.Token); }
                catch (Exception failure) { failures.Add(failure); }
            }
            try
            {
                foreach (PublicGenerationRun run in runs)
                {
                    await ObserveAsync(() => run.Stop ??= run.Channels.StopAsync("generation fixture final drain", CancellationToken.None));
                    await ObserveAsync(() => run.Channels.Completed);
                    await ObserveAsync(() => run.Operation);
                }
                foreach (GenerationBorrowedConnectionSupervisor parent in parents)
                {
                    await ObserveAsync(() => parent.StopAsync("generation fixture parent owner drain", CancellationToken.None));
                    await ObserveAsync(() => parent.Completed);
                    foreach (Task operation in parent.SendTasks) await ObserveAsync(() => operation);
                }
                foreach (RecordingConnectionContext context in roots.Concat(borrowedReplacements))
                {
                    if (context.CreationTask is { } creation) await ObserveAsync(() => creation);
                    if (context.CreationAgent is { } agent) await ObserveAsync(() => agent.Completed);
                }
                foreach (BoundaryConnectionFilter filter in filters)
                    if (filter.Admitted is { } admitted) await ObserveAsync(() => admitted);
                for (int index = 0; index < roots.Count; index++)
                    if (parents[index].SendCalls == 0)
                        await ObserveAsync(() => roots[index].DisposeAsync().AsTask());
                foreach (RecordingConnectionContext replacement in borrowedReplacements)
                    await ObserveAsync(() => replacement.DisposeAsync().AsTask());
                await ObserveAsync(() =>
                {
                    foreach (RecordingConnectionContext context in roots.Concat(borrowedReplacements))
                        Assert.Equal(1, context.DisposeCalls);
                    return Task.CompletedTask;
                });
            }
            finally { LogContext.Current = previous; }
            if (failures.Count > 0)
            {
                if (operationFailure is not null) failures.Insert(0, operationFailure);
                if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
                throw new AggregateException("Public generation test or owned cleanup failed.", failures);
            }
        }
    }

    private static RabbitMqReceiveEndpointContext CreatePublicGenerationEndpoint(RabbitMqBusConfiguration bus,
        IRabbitMqHostConfiguration host, string queue, CountingConnectionSpecification specification)
    {
        IRabbitMqEndpointConfiguration child = bus.CreateEndpointConfiguration(false);
        child.AutoStart = false;
        var settings = new RabbitMqReceiveSettings(child, queue, ExchangeType.Fanout, true, false);
        var endpoint = new RabbitMqReceiveEndpointConfiguration(host, settings, child);
        endpoint.ConfigureConnection(c => c.AddPipeSpecification(specification));
        return Assert.IsAssignableFrom<RabbitMqReceiveEndpointContext>(endpoint.CreateReceiveEndpointContext());
    }

    private static async Task CompleteAndStopGenerationAsync(PublicGenerationRun run, CancellationToken cancellationToken)
    {
        run.Raw.CreateRelease.TrySetResult();
        await run.Delivery.Entered.Task.WaitAsync(cancellationToken);
        Assert.Equal(1, run.Delivery.Calls);
        Assert.Same(run.Raw, run.Delivery.Context!.ConnectionContext);
        Assert.False(run.ParentOperation.IsCompleted);
        run.Delivery.Release.TrySetResult();
        await run.Operation.WaitAsync(cancellationToken);
        run.Stop = run.Channels.StopAsync("public generation completed", CancellationToken.None);
        await run.Raw.ChannelDisposal.Entered.Task.WaitAsync(cancellationToken);
        Assert.False(run.Stop.IsCompleted);
        Assert.False(run.ParentOperation.IsCompleted);
        Assert.Equal(1, run.Raw.ChannelDisposal.DisposeCalls);
        run.Raw.ChannelDisposal.Release.TrySetResult();
        await run.Stop.WaitAsync(cancellationToken);
        await run.Channels.Completed.WaitAsync(cancellationToken);
        await run.Raw.CreationAgent!.Completed.WaitAsync(cancellationToken);
        await run.ParentOperation.WaitAsync(cancellationToken);
    }

    private sealed class PublicGenerationRun
    {
        public PublicGenerationRun(IChannelContextSupervisor channels, GenerationBorrowedConnectionSupervisor parent,
            RecordingConnectionContext raw)
        {
            Channels = channels;
            Raw = raw;
            int index = parent.SendCalls;
            Operation = channels.SendAsync(Delivery, TestContext.Current.CancellationToken);
            ParentPublished = parent.GetRecordedAsync(index);
        }
        public IChannelContextSupervisor Channels { get; }
        public RecordingConnectionContext Raw { get; }
        public HeldPublicChannelPipe Delivery { get; } = new();
        public Task Operation { get; }
        public Task<Task> ParentPublished { get; }
        public Task ParentOperation { get; set; } = null!;
        public Task? Stop { get; set; }
        public void ReleaseAll()
        {
            Raw.CreateRelease.TrySetResult();
            Raw.ChannelDisposal.Release.TrySetResult();
            Delivery.Release.TrySetResult();
        }
    }

    private sealed class GenerationBorrowedConnectionSupervisor(IPipeContextFactory<ConnectionContext> factory)
        : ViciOne.ServiceBus.Transports.TransportPipeContextSupervisor<ConnectionContext>(factory), IConnectionContextSupervisor
    {
        private readonly List<Task> _sendTasks = [];
        private readonly TaskCompletionSource<Task>[] _recorded =
        [
            new(TaskCreationOptions.RunContinuationsAsynchronously),
            new(TaskCreationOptions.RunContinuationsAsynchronously)
        ];
        public int SendCalls { get { lock (_sendTasks) return _sendTasks.Count; } }
        public Task[] SendTasks { get { lock (_sendTasks) return _sendTasks.ToArray(); } }
        public Task<Task> GetRecordedAsync(int index) => _recorded[index].Task;
        public new Task SendAsync(IPipe<ConnectionContext> pipe, CancellationToken cancellationToken = default)
        {
            Task operation = base.SendAsync(pipe, cancellationToken);
            int index;
            lock (_sendTasks)
            {
                index = _sendTasks.Count;
                _sendTasks.Add(operation);
            }
            _recorded[index].TrySetResult(operation);
            return operation;
        }
        public Uri NormalizeAddress(Uri address) => throw Unexpected(nameof(NormalizeAddress));
        public Task<ViciOne.ServiceBus.Transports.ISendTransport> CreateSendTransportAsync(RabbitMqReceiveEndpointContext context,
            IChannelContextSupervisor channels, Uri address, CancellationToken cancellationToken = default)
            => throw Unexpected(nameof(CreateSendTransportAsync));
        public Task<ViciOne.ServiceBus.Transports.ISendTransport> CreatePublishTransportAsync<T>(RabbitMqReceiveEndpointContext context,
            IChannelContextSupervisor channels, CancellationToken cancellationToken = default) where T : class
            => throw Unexpected(nameof(CreatePublishTransportAsync));
    }

    private sealed class CountingConnectionSpecification(IFilter<ConnectionContext> filter) : IPipeSpecification<ConnectionContext>
    {
        private int _applyCalls;
        public int ApplyCalls => Volatile.Read(ref _applyCalls);
        public void Apply(IPipeBuilder<ConnectionContext> builder)
        {
            Interlocked.Increment(ref _applyCalls);
            builder.AddFilter(filter);
        }
        public IEnumerable<ValidationResult> Validate() => [];
    }
}
