using System.Collections.Concurrent;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using SqsQueue = ViciOne.ServiceBus.AmazonSqs.Topology.Queue;
using SnsTopic = ViciOne.ServiceBus.AmazonSqs.Topology.Topic;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsConnectionCreationMiddlewareLifetimeTests
{
    static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(10);
    static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-CONNECTION-CREATION-LIFETIME", "configured-creation-next-fault-cancel-and-unpublished-resource-ownership")]
    public async Task ConfiguredCreation_ControlsPublicationAndRetiresUnpublishedContextsAsync(int mode)
    {
        var shared = new SharedConnectionOwner();
        var fixture = new Fixture(shared, mode, "creation-orders");
        Task<ClientContext>? operation = null;
        try
        {
            operation = fixture.Factory.CreateContext(fixture.OwnerView).Context;
            ConnectionContext supplied = await fixture.Filter.Entered.Task.WaitAsync(Watchdog, TestToken);
            Assert.IsType<SharedConnectionContext>(supplied);
            Assert.Equal(fixture.CreationStop.Token, supplied.CancellationToken);
            Assert.False(operation.IsCompleted);
            Assert.Equal(0, shared.Connection.Creates);
            Assert.Equal(0, shared.Connection.Disposals);
            if (mode == 6)
                fixture.CreationStop.Cancel();
            fixture.Filter.Release.TrySetResult();
            if (mode == 0)
            {
                await fixture.Filter.AfterNext.Task.WaitAsync(Watchdog, TestToken);
                Assert.Equal(1, shared.Connection.Creates);
                Assert.False(operation.IsCompleted);
                Assert.False(Assert.Single(shared.ActualBridgeTasks).IsCompleted);
                Assert.Equal(0, shared.Connection.Disposals);
                fixture.Filter.PostRelease.TrySetResult();
            }
            Exception? failure = await Record.ExceptionAsync(async () => await operation.WaitAsync(Watchdog, TestToken));
            Assert.IsNotType<TimeoutException>(failure);
            Assert.Equal(1, fixture.Filter.Calls);
            Assert.Equal(0, shared.Connection.Disposals);
            if (mode is 0 or 7)
            {
                Assert.Null(failure);
                ClientContext result = await operation;
                Assert.Equal(1, shared.Connection.Creates);
                Assert.Equal(fixture.CreationStop.Token, shared.Connection.LastCreateToken);
                if (mode == 0)
                {
                    var scope = Assert.IsType<ScopeClientContext>(result);
                    Assert.Same(shared.Connection.LastPlainClient, scope.ParentClientContext);
                    Assert.IsNotType<ScopeClientContext>(scope.ParentClientContext);
                }
                else
                {
                    var owned = Assert.IsType<OwnedClient>(result);
                    Assert.Equal(0, owned.Disposals);
                    await fixture.Owner.StopAsync("release published client", CancellationToken.None).WaitAsync(Watchdog, TestToken);
                    Assert.Equal(1, owned.Disposals);
                    Assert.Equal(0, shared.Connection.Disposals);
                }
            }
            else if (mode == 6)
            {
                Assert.Equal(fixture.CreationStop.Token, Assert.IsAssignableFrom<OperationCanceledException>(failure).CancellationToken);
                Assert.Equal(0, shared.Connection.Creates);
            }
            else if (mode is 1 or 2)
            {
                var invalid = Assert.IsType<InvalidOperationException>(failure);
                Assert.Contains(mode == 1 ? "without creating" : "more than once", invalid.Message, StringComparison.Ordinal);
                Assert.Equal(mode == 1 ? 0 : 1, shared.Connection.Creates);
                if (mode == 2)
                    Assert.Equal(1, shared.Connection.LastOwnedClient!.Disposals);
            }
            else if (mode == 5)
            {
                var aggregate = Assert.IsType<AggregateException>(failure);
                Assert.Collection(aggregate.InnerExceptions,
                    cause => Assert.Same(fixture.Primary, cause),
                    cause => Assert.Same(fixture.Cleanup, cause));
                Assert.Equal(1, shared.Connection.Creates);
                Assert.Equal(1, shared.Connection.LastOwnedClient!.Disposals);
            }
            else
            {
                Assert.Same(fixture.Primary, failure);
                Assert.Equal(mode == 3 ? 0 : 1, shared.Connection.Creates);
                if (mode == 4)
                    Assert.Equal(1, shared.Connection.LastOwnedClient!.Disposals);
            }
            if (mode == 0)
            {
                Task actualBridge = Assert.Single(shared.ActualBridgeTasks);
                Assert.False(actualBridge.IsCompleted);
                await fixture.Owner.StopAsync("release held creation ownership", CancellationToken.None).WaitAsync(Watchdog, TestToken);
                await actualBridge.WaitAsync(Watchdog, TestToken);
                Assert.Equal(fixture.CreationStop.Token, (await operation).CancellationToken);
                Assert.Equal(0, shared.Connection.Disposals);
            }
        }
        finally
        {
            try { await fixture.DrainAsync(operation); }
            finally { await shared.DrainAsync(); }
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-CONNECTION-CREATION-LIFETIME", "two-endpoint-configurations-share-connection-with-separate-client-owners")]
    public async Task TwoEndpointBuilders_KeepTheirConfiguredFiltersAndClientOwnersSeparateAsync()
    {
        var shared = new SharedConnectionOwner();
        var first = new Fixture(shared, 7, "creation-first");
        var second = new Fixture(shared, 7, "creation-second");
        Task<ClientContext>? firstOperation = null;
        Task<ClientContext>? secondOperation = null;
        try
        {
            firstOperation = first.Factory.CreateContext(first.OwnerView).Context;
            secondOperation = second.Factory.CreateContext(second.OwnerView).Context;
            await first.Filter.Entered.Task.WaitAsync(Watchdog, TestToken);
            await second.Filter.Entered.Task.WaitAsync(Watchdog, TestToken);
            Assert.Equal(1, first.Filter.Calls);
            Assert.Equal(1, second.Filter.Calls);
            Assert.Equal(0, shared.Connection.Creates);
            Assert.False(firstOperation.IsCompleted);
            Assert.False(secondOperation.IsCompleted);
            first.Filter.Release.TrySetResult();
            var firstClient = Assert.IsType<OwnedClient>(await firstOperation.WaitAsync(Watchdog, TestToken));
            Assert.False(secondOperation.IsCompleted);
            Assert.Equal(1, shared.Connection.Creates);
            second.Filter.Release.TrySetResult();
            var secondClient = Assert.IsType<OwnedClient>(await secondOperation.WaitAsync(Watchdog, TestToken));
            Assert.NotSame(firstClient, secondClient);
            Assert.Equal(2, shared.Connection.Creates);
            await first.Owner.StopAsync("release first endpoint client", CancellationToken.None).WaitAsync(Watchdog, TestToken);
            Assert.Equal(1, firstClient.Disposals);
            Assert.Equal(0, secondClient.Disposals);
            Assert.Equal(0, shared.Connection.Disposals);
            await second.Owner.StopAsync("release second endpoint client", CancellationToken.None).WaitAsync(Watchdog, TestToken);
            Assert.Equal(1, secondClient.Disposals);
            Assert.Equal(0, shared.Connection.Disposals);
        }
        finally
        {
            try { await first.DrainAsync(firstOperation); }
            finally
            {
                try { await second.DrainAsync(secondOperation); }
                finally { await shared.DrainAsync(); }
            }
        }
    }

    static async Task ObserveJoinAsync(Task task)
    {
        Exception? failure = await Record.ExceptionAsync(() => task.WaitAsync(Watchdog, CancellationToken.None));
        Assert.IsNotType<TimeoutException>(failure);
    }

    sealed class Fixture
    {
        public readonly Supervisor Owner = new();
        public readonly CancellationTokenSource CreationStop = new();
        public readonly IOException Primary = new("connection middleware failure");
        public readonly IOException Cleanup = new("unpublished client cleanup failure");
        public readonly CreationFilter Filter;
        public readonly ClientContextFactory Factory;
        public readonly ISupervisor OwnerView;

        public Fixture(SharedConnectionOwner shared, int mode, string queueName)
        {
            Filter = new CreationFilter(shared.Connection, mode, Primary, Cleanup);
            var configuration = new AmazonSqsBusConfiguration(new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology()));
            configuration.HostConfiguration.Settings = new AmazonSqsHostConfigurator(new Uri("amazonsqs://eu-central-1/")).Settings;
            var endpoint = Assert.IsType<AmazonSqsReceiveEndpointConfiguration>(
                configuration.HostConfiguration.CreateReceiveEndpointConfiguration(queueName,
                    configure => configure.ConfigureConnection(pipe => pipe.UseFilter(Filter))));
            OwnerView = InterfaceProxy<ISupervisor>.Create((method, args) => method.Name switch
            {
                "get_Stopped" => CreationStop.Token,
                nameof(ISupervisor.Add) => AddOwnerAgent(args!),
                _ => throw new NotSupportedException("Unexpected owner boundary: " + method.Name),
            });
            Factory = new ClientContextFactory(shared.View, endpoint.BuildConnectionPipe());
        }
        object? AddOwnerAgent(object?[] args)
        {
            Owner.Add(Assert.IsAssignableFrom<IAgent>(Assert.Single(args)));
            return null;
        }
        public async Task DrainAsync(Task? operation)
        {
            Filter.Release.TrySetResult();
            Filter.PostRelease.TrySetResult();
            try
            {
                if (operation is not null)
                    await ObserveJoinAsync(operation);
            }
            finally
            {
                try { await ObserveJoinAsync(Owner.StopAsync("creation fixture cleanup", CancellationToken.None)); }
                finally { CreationStop.Dispose(); }
            }
        }
    }

    sealed class CreationFilter(ProbeConnection replacement, int mode, Exception primary, Exception cleanup) : IFilter<ConnectionContext>
    {
        int _calls;
        public int Calls => Volatile.Read(ref _calls);
        public TaskCompletionSource<ConnectionContext> Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource AfterNext { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource PostRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task SendAsync(ConnectionContext context, IPipe<ConnectionContext> next)
        {
            Interlocked.Increment(ref _calls);
            Entered.TrySetResult(context);
            await Release.Task.ConfigureAwait(false);
            if (mode == 1)
                return;
            if (mode == 3)
                throw primary;
            if (mode is 2 or 4 or 5 or 7)
            {
                replacement.NextOwnedCleanupFailure = mode == 5 ? cleanup : null;
                replacement.NextOwned = true;
                await next.SendAsync(replacement).ConfigureAwait(false);
                if (mode == 2)
                    await next.SendAsync(replacement).ConfigureAwait(false);
                if (mode is 4 or 5)
                    throw primary;
            }
            else
                await next.SendAsync(context).ConfigureAwait(false);
            AfterNext.TrySetResult();
            if (mode == 0)
                await PostRelease.Task.ConfigureAwait(false);
        }
        public void Probe(ProbeContext context) { }
    }

    sealed class SharedConnectionOwner
    {
        public readonly ProbeConnection Connection = new();
        public readonly PipeContextSupervisor<ConnectionContext> Supervisor;
        public readonly IConnectionContextSupervisor View;
        readonly ConcurrentQueue<Task> _actualBridgeTasks = new();
        public Task[] ActualBridgeTasks => _actualBridgeTasks.ToArray();
        public SharedConnectionOwner()
        {
            Supervisor = new PipeContextSupervisor<ConnectionContext>(new ProbeFactory(Connection));
            View = InterfaceProxy<IConnectionContextSupervisor>.Create((method, args) => method.Name switch
            {
                nameof(ISupervisor<ConnectionContext>.SendAsync) => SendAsync(args!),
                _ => throw new NotSupportedException("Unexpected connection supervisor boundary: " + method.Name),
            });
        }
        Task SendAsync(object?[] args)
        {
            Task actual = Supervisor.SendAsync(Assert.IsAssignableFrom<IPipe<ConnectionContext>>(args[0]), Assert.IsType<CancellationToken>(args[1]));
            _actualBridgeTasks.Enqueue(actual);
            return actual;
        }
        public async Task DrainAsync()
        {
            try { await ObserveJoinAsync(Supervisor.StopAsync("shared connection cleanup", CancellationToken.None)); }
            finally
            {
                foreach (Task actual in _actualBridgeTasks.ToArray())
                    await ObserveJoinAsync(actual);
            }
        }
    }

    sealed class ProbeFactory(ProbeConnection connection) : IPipeContextFactory<ConnectionContext>
    {
        public IPipeContextAgent<ConnectionContext> CreateContext(ISupervisor supervisor) =>
            supervisor.AddContext(Task.FromResult<ConnectionContext>(connection));
        public IActivePipeContextAgent<ConnectionContext> CreateActiveContext(ISupervisor supervisor,
            IPipeContextHandle<ConnectionContext> context, CancellationToken cancellationToken) =>
            supervisor.AddActiveContext(context, Task.FromResult<ConnectionContext>(new SharedConnectionContext(connection, cancellationToken)));
    }

    sealed class ProbeConnection() : BasePipeContext(CancellationToken.None), ConnectionContext, IDisposable
    {
        public int Creates;
        public int Disposals;
        public bool NextOwned;
        public Exception? NextOwnedCleanupFailure;
        public CancellationToken LastCreateToken;
        public ClientContext? LastPlainClient;
        public OwnedClient? LastOwnedClient;
        public IConnection Connection => throw new NotSupportedException();
        public Uri HostAddress => new("amazonsqs://eu-central-1/");
        public IAmazonSqsBusTopology Topology => throw new NotSupportedException();
        public ClientContext CreateClientContext(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Creates);
            LastCreateToken = cancellationToken;
            if (NextOwned)
                return LastOwnedClient = new OwnedClient(this, cancellationToken, NextOwnedCleanupFailure);
            return LastPlainClient = InterfaceProxy<ClientContext>.Create((method, _) => method.Name switch
            {
                "get_CancellationToken" => cancellationToken,
                "get_ConnectionContext" => this,
                _ => throw new NotSupportedException("Unexpected borrowed client boundary: " + method.Name),
            });
        }
        public void Dispose() => Interlocked.Increment(ref Disposals);
        public Task<QueueInfo> GetQueueAsync(SqsQueue queue, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<QueueInfo> GetQueueByNameAsync(string name, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> RemoveQueueByNameAsync(string name, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TopicInfo> GetTopicAsync(SnsTopic topic, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TopicInfo> GetTopicByNameAsync(string name, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> RemoveTopicByNameAsync(string name, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    sealed class OwnedClient : ScopeClientContext, IDisposable
    {
        readonly Exception? _cleanupFailure;
        public int Disposals;
        public OwnedClient(ProbeConnection connection, CancellationToken token, Exception? cleanupFailure)
            : base(InterfaceProxy<ClientContext>.Create((method, _) => method.Name switch
            {
                "get_CancellationToken" => token,
                "get_ConnectionContext" => connection,
                _ => throw new NotSupportedException("Unexpected owned client boundary: " + method.Name),
            }), token) => _cleanupFailure = cleanupFailure;
        void IDisposable.Dispose()
        {
            Interlocked.Increment(ref Disposals);
            base.Dispose();
            if (_cleanupFailure is not null)
                throw _cleanupFailure;
        }
    }
}
