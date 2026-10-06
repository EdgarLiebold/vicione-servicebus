using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class HostOwnedShutdownDrainTests
{
    static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [RequirementCoverage("REQ-VSB-HOST-LIFECYCLE", "shutdown-provider-failure-does-not-abandon-other-owned-stop-work")]
    public async Task Shutdown_AttemptsAndDrainsLaterOwnedWorkWhileReturningTheProviderFailureAsync(int route, bool providerFails)
    {
        ILogContext? previous = LogContext.Current;
        Fixture? fixture = null;
        Exception? primary = null;
        var cleanup = new List<Exception>();
        try
        {
            LogContext.ConfigureCurrentLogContext();
            fixture = new Fixture(route, providerFails, LogContext.Current!);
            await fixture.StartAsync();
            Task operation = fixture.StopAsync();
            await fixture.First.Entered.Task.WaitAsync(Bound, CancellationToken.None);
            Assert.Equal(1, fixture.First.Calls);
            Assert.False(fixture.First.FirstRaw.IsCompleted);
            Assert.False(operation.IsCompleted);
            Assert.Equal(0, fixture.Second.Calls);
            fixture.First.Release();
            await Task.WhenAny(operation, fixture.Second.Entered.Task).WaitAsync(Bound, CancellationToken.None);
            if (providerFails)
            {
                Assert.True(fixture.First.FirstRaw.IsFaulted);
                Assert.Same(fixture.Failure, Assert.Single(fixture.First.FirstRaw.Exception!.InnerExceptions));
                Assert.False(fixture.First.Completed.IsCompleted);
                if (operation.IsCompleted)
                {
                    Exception? terminal = await Record.ExceptionAsync(() => operation.WaitAsync(Bound, CancellationToken.None));
                    Assert.True(operation.IsFaulted);
                    Assert.True(fixture.IsOnlyExpectedFailure(terminal));
                }
            }
            else
                Assert.True(fixture.First.FirstRaw.IsCompletedSuccessfully);

            // FIRST ownership oracle, before any fallback stop or later gate release.
            Assert.Equal(1, fixture.Second.Calls);
            Assert.False(fixture.Second.FirstRaw.IsCompleted);
            Assert.False(operation.IsCompleted);
            fixture.Second.Release();
            if (fixture.Third is { } third)
            {
                await third.Entered.Task.WaitAsync(Bound, CancellationToken.None);
                Assert.Equal(1, third.Calls);
                Assert.False(third.FirstRaw.IsCompleted);
                Assert.False(operation.IsCompleted);
                third.Release();
            }
            Exception? observed = await Record.ExceptionAsync(() => operation.WaitAsync(Bound, CancellationToken.None));
            if (providerFails)
            {
                Assert.True(operation.IsFaulted);
                Assert.True(fixture.IsOnlyExpectedFailure(observed));
            }
            else
            {
                Assert.Null(observed);
                Assert.True(operation.IsCompletedSuccessfully);
            }
            Assert.True(fixture.Second.FirstRaw.IsCompletedSuccessfully);
            Assert.True(fixture.Second.Completed.IsCompletedSuccessfully);
            Assert.All(fixture.Stages, stage => Assert.Equal(1, stage.Calls));
            Assert.All(fixture.Endpoints, endpoint =>
                Assert.Equal(route == 2 && providerFails && endpoint.Name == "application" ? 0 : 1, endpoint.ResetCalls));
        }
        catch (Exception exception) { primary = exception; }
        finally
        {
            try
            {
                if (fixture is not null)
                    await fixture.RetireAsync(cleanup);
            }
            catch (Exception exception) { cleanup.Add(exception); }
            finally { LogContext.Current = previous; }
        }
        if (cleanup.Count != 0)
        {
            if (primary is not null)
                cleanup.Insert(0, primary);
            throw new AggregateException("Shutdown control and retirement failed.", cleanup);
        }
        if (primary is not null)
            ExceptionDispatchInfo.Capture(primary).Throw();
    }

    static async Task CaptureAsync(Func<Task> operation, List<Exception> failures)
    {
        try { await operation(); }
        catch (Exception exception) { failures.Add(exception); }
    }

    sealed class Fixture
    {
        public readonly IOException Failure = new("unique admitted provider stop failure");
        public readonly List<StopStage> Stages = new();
        public readonly List<EndpointFixture> Endpoints = new();
        public readonly List<OwnedAgent> Agents = new();
        public readonly List<Task> PublicTasks = new();
        public readonly StopStage First;
        public readonly StopStage Second;
        public readonly StopStage? Third;
        readonly int _route;
        readonly ReceiveEndpointCollection? _collection;
        readonly FixtureHost? _host;
        readonly FixtureRider? _rider;
        IHostHandle? _hostHandle;

        public Fixture(int route, bool providerFails, ILogContext logContext)
        {
            _route = route;
            StopStage Stage(bool held, bool fails = false)
            {
                var stage = new StopStage(held, fails, Failure);
                Stages.Add(stage);
                return stage;
            }
            if (route == 2)
            {
                First = Stage(true, providerFails);
                Second = Stage(true);
                Endpoints.Add(new EndpointFixture("application", false, First, logContext));
                Endpoints.Add(new EndpointFixture("bus", true, Second, logContext));
                _collection = new ReceiveEndpointCollection();
                foreach (EndpointFixture endpoint in Endpoints)
                    _collection.Add(endpoint.Name, endpoint.Endpoint);
                return;
            }
            StopStage rider = Stage(route == 0, route == 0 && providerFails);
            StopStage endpointStage = Stage(route == 0);
            var endpointFixture = new EndpointFixture("application", false, endpointStage, logContext);
            Endpoints.Add(endpointFixture);
            _rider = new FixtureRider(rider);
            if (route == 0)
            {
                First = rider;
                Second = endpointStage;
                Third = Stage(true);
                Agents.Add(new OwnedAgent(Third));
            }
            else if (route == 1)
            {
                First = Stage(true, providerFails);
                Second = Stage(true);
                Agents.Add(new OwnedAgent(First));
                Agents.Add(new OwnedAgent(Second));
            }
            else
                throw new ArgumentOutOfRangeException(nameof(route));
            var configuration = PublicProxy<IHostConfiguration>.Create((method, _) => method.Name switch
            {
                "get_HostAddress" => new Uri("loopback://localhost/"),
                "get_LogContext" => logContext,
                _ => throw new NotSupportedException("Unexpected host configuration boundary: " + method.Name)
            });
            var topology = PublicProxy<IBusTopology>.Create((method, _) => throw new NotSupportedException(method.Name));
            _host = new FixtureHost(configuration, topology, Agents.ToArray());
            _host.AddReceiveEndpoint(endpointFixture.Name, endpointFixture.Endpoint);
            _host.AddRider("owned-rider", _rider);
        }

        public async Task StartAsync()
        {
            if (_route == 2)
            {
                IHostReceiveEndpointHandle[] handles = _collection!.StartEndpoints(CancellationToken.None);
                Assert.Equal(2, handles.Length);
                foreach (IHostReceiveEndpointHandle handle in handles)
                    PublicTasks.Add(handle.Ready);
                foreach (EndpointFixture endpoint in Endpoints)
                    PublicTasks.Add(endpoint.NotifyReadyAsync());
                foreach (IHostReceiveEndpointHandle handle in handles)
                {
                    ReceiveEndpointReady ready = await handle.Ready.WaitAsync(Bound, CancellationToken.None);
                    Assert.Same(handle.ReceiveEndpoint, ready.ReceiveEndpoint);
                }
            }
            else
            {
                _hostHandle = _host!.Start(CancellationToken.None);
                PublicTasks.Add(_hostHandle.Ready);
                foreach (EndpointFixture endpoint in Endpoints)
                    PublicTasks.Add(endpoint.NotifyReadyAsync());
                HostReady ready = await _hostHandle.Ready.WaitAsync(Bound, CancellationToken.None);
                Assert.Equal(_host.Address, ready.HostAddress);
                Assert.Same(Endpoints[0].Endpoint, Assert.Single(ready.ReceiveEndpoints).ReceiveEndpoint);
                Assert.Equal("owned-rider", Assert.Single(ready.Riders).Name);
                Assert.Equal(1, _rider!.Starts);
                Assert.Same(_rider, _host.GetRider("owned-rider"));
                foreach (OwnedAgent agent in Agents)
                {
                    PublicTasks.Add(agent.Ready);
                    await agent.Ready.WaitAsync(Bound, CancellationToken.None);
                    Assert.False(agent.Completed.IsCompleted);
                }
            }
            foreach (EndpointFixture endpoint in Endpoints)
            {
                PublicTasks.Add(endpoint.Endpoint.Started);
                await endpoint.Endpoint.Started.WaitAsync(Bound, CancellationToken.None);
                Assert.Equal(1, endpoint.Starts);
                Assert.Equal(ReceiveEndpoint.State.Ready, endpoint.Endpoint.CurrentState);
            }
            foreach (Task task in PublicTasks)
                await task.WaitAsync(Bound, CancellationToken.None);
        }

        public Task StopAsync()
        {
            Task operation = _route == 2 ? _collection!.StopEndpointsAsync(CancellationToken.None)
                : _hostHandle!.StopAsync(CancellationToken.None);
            PublicTasks.Add(operation);
            return operation;
        }

        public bool IsOnlyExpectedFailure(Exception? exception) => ReferenceEquals(exception, Failure)
            || exception is AggregateException aggregate && aggregate.InnerExceptions.Count > 0
                && aggregate.InnerExceptions.All(IsOnlyExpectedFailure);

        async Task ObserveAsync(Task task)
        {
            try { await task.WaitAsync(Bound, CancellationToken.None); }
            catch (Exception exception) when (task.IsFaulted && IsOnlyExpectedFailure(exception)) { }
        }

        public async Task RetireAsync(List<Exception> failures)
        {
            foreach (StopStage stage in Stages)
                stage.DisarmAndRelease();
            foreach (Task task in PublicTasks.ToArray())
                await CaptureAsync(() => ObserveAsync(task), failures);
            // These are public fallback retirements, after the first causal assertion.
            foreach (EndpointFixture endpoint in Endpoints)
                await CaptureAsync(async () =>
                {
                    Task stop = endpoint.Endpoint.StopAsync(CancellationToken.None);
                    PublicTasks.Add(stop);
                    await stop.WaitAsync(Bound, CancellationToken.None);
                }, failures);
            if (_rider is not null)
                await CaptureAsync(async () =>
                {
                    Task stop = _rider.Handle.StopAsync(CancellationToken.None);
                    PublicTasks.Add(stop);
                    await stop.WaitAsync(Bound, CancellationToken.None);
                }, failures);
            foreach (OwnedAgent agent in Agents)
            {
                await CaptureAsync(async () =>
                {
                    Task stop = agent.StopAsync("fixture fallback retirement", CancellationToken.None);
                    PublicTasks.Add(stop);
                    await stop.WaitAsync(Bound, CancellationToken.None);
                }, failures);
                await CaptureAsync(() => agent.Completed.WaitAsync(Bound, CancellationToken.None), failures);
            }
            await CaptureAsync(async () =>
            {
                Task retry = _route == 2 ? _collection!.StopEndpointsAsync(CancellationToken.None)
                    : _host!.StopAsync(CancellationToken.None);
                PublicTasks.Add(retry);
                await retry.WaitAsync(Bound, CancellationToken.None);
            }, failures);
            foreach (StopStage stage in Stages)
            {
                foreach (Task task in stage.ActualTasks.ToArray())
                    await CaptureAsync(() => ObserveAsync(task), failures);
                await CaptureAsync(() => stage.Completed.WaitAsync(Bound, CancellationToken.None), failures);
            }
            foreach (Task task in PublicTasks.ToArray())
                await CaptureAsync(() => ObserveAsync(task), failures);
        }
    }

    sealed class StopStage
    {
        readonly TaskCompletionSource _first = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly bool _fails;
        readonly Exception _failure;
        bool _armed = true;
        int _calls;
        public readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly ConcurrentQueue<Task> ActualTasks = new();
        public int Calls => Volatile.Read(ref _calls);
        public Task FirstRaw => _first.Task;
        public Task Completed => _completed.Task;

        public StopStage(bool held, bool fails, Exception failure)
        {
            _fails = fails;
            _failure = failure;
            if (!held)
                _first.TrySetResult();
        }
        public Task StopAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            int call = Interlocked.Increment(ref _calls);
            Task raw = call == 1 ? _first.Task : Task.CompletedTask;
            ActualTasks.Enqueue(raw);
            Task operation = CompleteAsync(raw);
            ActualTasks.Enqueue(operation);
            Entered.TrySetResult();
            return operation;
        }
        async Task CompleteAsync(Task raw)
        {
            await raw.ConfigureAwait(false);
            _completed.TrySetResult();
        }
        public void Release()
        {
            if (_armed && _fails)
                _first.TrySetException(_failure);
            else
                _first.TrySetResult();
        }
        public void DisarmAndRelease()
        {
            _armed = false;
            Release();
        }
    }

    sealed class OwnedAgent : Agent
    {
        readonly StopStage _stage;
        public OwnedAgent(StopStage stage)
        {
            _stage = stage;
            SetReady();
        }
        protected override async Task StopAgentAsync(StopContext context)
        {
            await _stage.StopAsync(context.CancellationToken).ConfigureAwait(false);
            await base.StopAgentAsync(context).ConfigureAwait(false);
        }
    }

    sealed class FixtureRider(StopStage stage) : IRiderControl
    {
        public int Starts;
        public RiderHandle Handle { get; } = new ControlledRiderHandle(stage);
        public RiderHandle Start(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref Starts);
            return Handle;
        }
        public IEnumerable<EndpointHealthResult> CheckEndpointHealth() => [];
        sealed class ControlledRiderHandle(StopStage stage) : RiderHandle
        {
            public Task Ready => Task.CompletedTask;
            public Task StopAsync(CancellationToken cancellationToken) => stage.StopAsync(cancellationToken);
        }
    }

    sealed class EndpointFixture
    {
        readonly ReceiveTransportObservable _transportObservers = new();
        readonly ReceiveEndpointObservable _endpointObservers = new();
        readonly StopStage _stage;
        public readonly string Name;
        public readonly Uri Address;
        public readonly ReceiveEndpoint Endpoint;
        public int Starts;
        public int ResetCalls;
        public EndpointFixture(string name, bool isBusEndpoint, StopStage stage, ILogContext logContext)
        {
            Name = name;
            Address = new Uri("loopback://localhost/" + name);
            _stage = stage;
            var context = PublicProxy<ReceiveEndpointContext>.Create((method, args) => method.Name switch
            {
                "get_InputAddress" => Address,
                "get_IsBusEndpoint" => isBusEndpoint,
                "get_LogContext" => logContext,
                "get_EndpointObservers" => _endpointObservers,
                "get_DependentsCompleted" => Task.CompletedTask,
                nameof(IReceiveEndpointObserverConnector.ConnectReceiveEndpointObserver) => _endpointObservers.Connect((IReceiveEndpointObserver)args![0]!),
                nameof(ReceiveEndpointContext.ResetAsync) => ResetAsync(),
                _ => throw new NotSupportedException("Unexpected receive-context boundary: " + method.Name)
            });
            var transport = PublicProxy<IReceiveTransport>.Create((method, args) => method.Name switch
            {
                nameof(IReceiveTransportObserverConnector.ConnectReceiveTransportObserver) => _transportObservers.Connect((IReceiveTransportObserver)args![0]!),
                nameof(IReceiveTransport.Start) => Start(),
                _ => throw new NotSupportedException("Unexpected receive-transport boundary: " + method.Name)
            });
            Endpoint = new ReceiveEndpoint(transport, context);
        }
        ReceiveTransportHandle Start()
        {
            Interlocked.Increment(ref Starts);
            return new TransportHandle(_stage);
        }
        ValueTask ResetAsync()
        {
            Interlocked.Increment(ref ResetCalls);
            return ValueTask.CompletedTask;
        }
        public Task NotifyReadyAsync() => _transportObservers.ReadyAsync(new ReadyEvent(Address));
        sealed record ReadyEvent(Uri InputAddress) : ReceiveTransportReady
        {
            public bool IsStarted => true;
        }
        sealed class TransportHandle(StopStage stage) : ReceiveTransportHandle
        {
            public Task StopAsync(CancellationToken cancellationToken = default) => stage.StopAsync(cancellationToken);
        }
    }

    sealed class FixtureHost(IHostConfiguration configuration, IBusTopology topology, IAgent[] agents) : BaseHost(configuration, topology)
    {
        public override IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition,
            IEndpointNameFormatter? endpointNameFormatter, Action<IReceiveEndpointConfigurator>? configureEndpoint = null) => throw new NotSupportedException();
        public override IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName,
            Action<IReceiveEndpointConfigurator>? configureEndpoint = null) => throw new NotSupportedException();
        protected override IAgent[] GetAgentHandles() => agents;
        protected override void Probe(ProbeContext context) { }
    }

    class PublicProxy<T> : DispatchProxy where T : class
    {
        Func<MethodInfo, object?[]?, object?>? _handler;
        public static T Create(Func<MethodInfo, object?[]?, object?> handler)
        {
            T value = Create<T, PublicProxy<T>>();
            ((PublicProxy<T>)(object)value)._handler = handler;
            return value;
        }
        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            (_handler ?? throw new InvalidOperationException("Public SPI handler missing"))(
                method ?? throw new InvalidOperationException("Public SPI method missing"), args);
    }
}
