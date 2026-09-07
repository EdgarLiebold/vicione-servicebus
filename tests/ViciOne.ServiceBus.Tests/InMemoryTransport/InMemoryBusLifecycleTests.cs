using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.InMemoryTransport;

public sealed class InMemoryBusLifecycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-CONTROL-LIFECYCLE", "bounded-async-only-api")]
    public async Task BoundedLifecycleExtensions_AreAsyncOnlyAndRejectInvalidBoundariesAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IBusControl bus = Bus.Factory.CreateUsingInMemory(_ => { });
        MethodInfo[] publicMethods = typeof(BusControlExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static);

        Assert.DoesNotContain(publicMethods, method =>
            (method.Name == "Start" || method.Name == "Stop") && method.ReturnType == typeof(void));
        Assert.Equal("bus", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            BusControlExtensions.StartAsync(null!, TimeSpan.FromSeconds(1), cancellationToken: cancellationToken))).ParamName);
        Assert.Equal("startTimeout", (await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            bus.StartAsync(TimeSpan.Zero, cancellationToken: cancellationToken))).ParamName);
        Assert.Equal("stopTimeout", (await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            bus.StopAsync(TimeSpan.FromMilliseconds(-1), cancellationToken: cancellationToken))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-CONTROL-LIFECYCLE", "bounded-operations-use-supplied-clock")]
    public async Task BoundedLifecycleExtensions_UseTheSuppliedClockAsync()
    {
        var clock = new FakeTimeProvider();
        IBusControl bus = DispatchProxy.Create<IBusControl, LifecycleTimeoutProxy>();

        Task start = bus.StartAsync(TimeSpan.FromMinutes(1), clock, TestContext.Current.CancellationToken);
        Assert.False(start.IsCompleted);

        clock.Advance(TimeSpan.FromMinutes(1));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start);

        Task stop = bus.StopAsync(TimeSpan.FromMinutes(2), clock, TestContext.Current.CancellationToken);
        Assert.False(stop.IsCompleted);

        clock.Advance(TimeSpan.FromMinutes(2));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stop);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-CONTROL-LIFECYCLE", "deploy-stop-has-independent-cleanup-token")]
    public async Task DeployTopology_StopsAfterTheStartupCallerTokenIsCanceledAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var observer = new CancelAfterStartObserver(cancellation);
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration => configuration.ConnectBusObserver(observer));

        await bus.DeployAsync(cancellation.Token);

        Assert.True(cancellation.IsCancellationRequested);
        Assert.Equal(1, observer.PreStopCount);
        Assert.Equal(1, observer.PostStopCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-LIFECYCLE", "concurrent-start-has-one-owner")]
    public async Task ConcurrentStartCalls_ShareOneLifecycleTransitionAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observer = new GatedBusStartObserver();
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration => configuration.ConnectBusObserver(observer));

        Task firstStart = bus.StartAsync(cancellationToken);
        await observer.Entered.WaitAsync(timeout, cancellationToken);
        Task secondStart = bus.StartAsync(cancellationToken);

        try
        {
            Assert.Equal(1, observer.PreStartCount);

            observer.Release();
            await Task.WhenAll(firstStart, secondStart).WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            observer.Release();

            try
            {
                await Task.WhenAll(firstStart, secondStart).WaitAsync(timeout, CancellationToken.None);
            }
            catch
            {
                // The assertion result remains authoritative; stopping below owns any started host.
            }

            await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(1, observer.PreStartCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-LIFECYCLE", "canceled-start-propagates-and-remains-retryable")]
    public async Task CanceledStartup_PropagatesCancellationAndAllowsALaterStartAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        var dependency = new ControllableDependency();
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration =>
            configuration.ReceiveEndpoint(
                $"canceled-start-{NewId.NextGuid():N}",
                endpoint => endpoint.AddDependency(dependency)));
        using var startCancellation = new CancellationTokenSource();

        try
        {
            Task firstStart = bus.StartAsync(startCancellation.Token);
            await dependency.Waiting.WaitAsync(timeout, testCancellation);
            startCancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                firstStart.WaitAsync(timeout, testCancellation));

            dependency.Complete();
            await bus.StartAsync(testCancellation).WaitAsync(timeout, testCancellation);
        }
        finally
        {
            dependency.Complete();
            await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-LIFECYCLE", "canceled-stop-retains-lifecycle-owner")]
    public async Task CanceledStop_RetainsTheHandleSoASecondStopCanCompleteAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observer = new GatedFirstBusStopObserver();
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration => configuration.ConnectBusObserver(observer));
        await bus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        using var stopCancellation = new CancellationTokenSource();

        Task firstStop = bus.StopAsync(stopCancellation.Token);
        await observer.Entered.WaitAsync(timeout, cancellationToken);
        stopCancellation.Cancel();
        observer.Release();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => firstStop);
        await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);

        Assert.Equal(2, observer.PreStopCount);
        Assert.Equal(1, observer.PostStopCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-LIFECYCLE", "concurrent-stop-has-one-owner")]
    public async Task ConcurrentStopCalls_ShareOneLifecycleTransitionAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observer = new GatedFirstBusStopObserver();
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration => configuration.ConnectBusObserver(observer));
        await bus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);

        Task firstStop = bus.StopAsync(CancellationToken.None);
        await observer.Entered.WaitAsync(timeout, cancellationToken);
        Task secondStop = bus.StopAsync(CancellationToken.None);

        Assert.Equal(1, observer.PreStopCount);

        observer.Release();
        await Task.WhenAll(firstStop, secondStop).WaitAsync(timeout, CancellationToken.None);

        Assert.Equal(1, observer.PreStopCount);
        Assert.Equal(1, observer.PostStopCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-LIFECYCLE", "stop-fault-is-observed-once")]
    public async Task StopFailure_NotifiesObserversExactlyOnceAndRemainsRetryableAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observer = new FaultingFirstPostStopObserver();
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration => configuration.ConnectBusObserver(observer));
        await bus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);

        ExpectedStopException exception = await Assert.ThrowsAsync<ExpectedStopException>(() =>
            bus.StopAsync(cancellationToken).WaitAsync(timeout, cancellationToken));

        Assert.Same(observer.ExpectedFailure, exception);
        Assert.Equal(1, observer.StopFaultedCount);
        Assert.Same(exception, observer.ObservedFailure);

        await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        Assert.Equal(2, observer.PostStopCount);
        Assert.Equal(1, observer.StopFaultedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-LIFECYCLE", "start-fault-is-observed-once-and-preserved")]
    public async Task StartObserverFailure_PreservesTheOriginalFailureAndRemainsRetryableAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observer = new FaultingFirstPreStartObserver();
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration => configuration.ConnectBusObserver(observer));

        ExpectedStartObserverException exception = await Assert.ThrowsAsync<ExpectedStartObserverException>(() =>
            bus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken));

        Assert.Same(observer.ExpectedFailure, exception);
        Assert.Equal(1, observer.StartFaultedCount);
        Assert.Same(exception, observer.ObservedFailure);

        await bus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        Assert.Equal(2, observer.PreStartCount);
        Assert.Equal(1, observer.StartFaultedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-LIFECYCLE", "stop-awaits-owned-startup-observation")]
    public async Task Stop_WaitsForTheOwnedStartupObservationBeforeCompletingAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        const string endpointName = "owned-startup";
        var observer = new GatedReadyObserver(endpointName);
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration =>
            configuration.ReceiveEndpoint(endpointName, endpoint => endpoint.Handler<BlockedMessage>(_ => Task.CompletedTask)));
        using ConnectHandle observerHandle = bus.ConnectReceiveEndpointObserver(observer);

        Task start = bus.StartAsync(cancellationToken);
        IReceiveEndpoint endpoint = await observer.Entered.WaitAsync(timeout, cancellationToken);
        ReceiveTransportHandle transportHandle = GetTransportHandle(endpoint);
        await GetExecutor(transportHandle).DisposeAsync();
        Task stop = transportHandle.StopAsync(CancellationToken.None);

        Assert.False(stop.IsCompleted);
        Assert.False(observer.CompletedObserved.IsCompleted);
        observer.Release();
        await start.WaitAsync(timeout, cancellationToken);
        await stop.WaitAsync(timeout, CancellationToken.None);
        await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);

        Assert.Equal(1, observer.ReadyCount);
        Assert.True(observer.CompletedObserved.IsCompletedSuccessfully);
        Assert.True(stop.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-LIFECYCLE", "startup-fault-is-terminal-and-stop-is-owned")]
    public async Task StartupDependencyFault_IsPublishedAsTerminalAndTheBusStillStopsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var dependency = new FailingDependency();
        var observer = new StartupObserver("Blocked");
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IReceiveEndpointDependency>(dependency)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<BlockedConsumer, BlockedConsumerDefinition>();
            })
            .AddOptions<ViciOneServiceBusHostOptions>()
            .Configure(options => options.WaitUntilStarted = false)
            .Services
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = provider.GetTestHarness();
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        using ConnectHandle observerHandle = bus.ConnectReceiveEndpointObserver(observer);
        var expected = new ExpectedStartupException();

        await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        dependency.Fail(expected);
        ReceiveEndpointFaulted fault = await observer.FaultObserved.WaitAsync(timeout, cancellationToken);
        await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);

        Assert.True(fault.IsTerminal);
        Assert.Same(expected, fault.Exception);
        Assert.False(observer.ReadyObserved.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-LIFECYCLE", "start-request-stop-and-restart")]
    public async Task StartRequestStopAndRestart_PreservesExactRequestRoutingAcrossBothRunsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration =>
            configuration.ReceiveEndpoint($"lifecycle-{NewId.NextGuid():N}", endpoint =>
                endpoint.Handler<LifecycleRequest>(context =>
                    context.RespondAsync(new LifecycleResponse(context.Message.Id, context.Message.Run)))));

        LifecycleResponse first = await RunOnceAsync(bus, new LifecycleRequest(NewId.NextGuid(), 1), timeout, cancellationToken);
        LifecycleResponse second = await RunOnceAsync(bus, new LifecycleRequest(NewId.NextGuid(), 2), timeout, cancellationToken);

        Assert.Equal(1, first.Run);
        Assert.Equal(2, second.Run);
        Assert.NotEqual(first.Id, second.Id);
    }

    private static async Task<LifecycleResponse> RunOnceAsync(
        IBusControl bus,
        LifecycleRequest request,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        await bus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Response<LifecycleResponse> response = await bus.CreateRequestClient<LifecycleRequest>()
                .GetResponseAsync<LifecycleResponse>(request, cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            Assert.Equal(request.Id, response.Message.Id);
            Assert.Equal(request.Run, response.Message.Run);
            return response.Message;
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static ReceiveTransportHandle GetTransportHandle(IReceiveEndpoint endpoint)
    {
        var receiveEndpoint = Assert.IsType<ReceiveEndpoint>(endpoint);
        FieldInfo handleField = typeof(ReceiveEndpoint).GetField("_handle", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("The receive endpoint handle field was not found.");
        object? endpointHandle = handleField.GetValue(receiveEndpoint);
        Assert.NotNull(endpointHandle);
        PropertyInfo transportHandleProperty = endpointHandle.GetType().GetProperty("TransportHandle", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("The receive transport handle property was not found.");

        return Assert.IsAssignableFrom<ReceiveTransportHandle>(transportHandleProperty.GetValue(endpointHandle));
    }

    private static TaskExecutor GetExecutor(ReceiveTransportHandle transportHandle)
    {
        FieldInfo executorField = transportHandle.GetType().GetField("_executor", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("The in-memory transport executor field was not found.");

        return Assert.IsType<TaskExecutor>(executorField.GetValue(transportHandle));
    }

    public class LifecycleTimeoutProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            CancellationToken cancellationToken = Assert.IsType<CancellationToken>(args![0]);

            return targetMethod.Name switch
            {
                nameof(IBusControl.StartAsync) or nameof(IBusControl.StopAsync) => WaitForCancellationAsync(cancellationToken),
                _ => throw new InvalidOperationException($"Unexpected bus-control member: {targetMethod.Name}."),
            };
        }

        private static async Task WaitForCancellationAsync(CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using CancellationTokenRegistration registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
            await completion.Task;
        }
    }

    public sealed class FailingDependency : IReceiveEndpointDependency
    {
        private readonly TaskCompletionSource _ready =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Ready => _ready.Task;

        public void Fail(Exception exception) => _ready.TrySetException(exception);
    }

    private sealed class ControllableDependency : IReceiveEndpointDependency
    {
        private readonly TaskCompletionSource _ready =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _waiting =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Ready
        {
            get
            {
                _waiting.TrySetResult();
                return _ready.Task;
            }
        }

        public Task Waiting => _waiting.Task;

        public void Complete() => _ready.TrySetResult();
    }

    public sealed class BlockedConsumer : IConsumer<BlockedMessage>
    {
        public Task ConsumeAsync(ConsumeContext<BlockedMessage> context) => Task.CompletedTask;
    }

    public sealed class BlockedConsumerDefinition(IReceiveEndpointDependency dependency) : ConsumerDefinition<BlockedConsumer>
    {
        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<BlockedConsumer> consumerConfigurator,
            IRegistrationContext context) => endpointConfigurator.AddDependency(dependency);
    }

    private sealed class StartupObserver(string endpointName) : IReceiveEndpointObserver
    {
        private readonly TaskCompletionSource<ReceiveEndpointFaulted> _faulted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<ReceiveEndpointReady> _ready =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<ReceiveEndpointFaulted> FaultObserved => _faulted.Task;
        public Task<ReceiveEndpointReady> ReadyObserved => _ready.Task;

        public Task ReadyAsync(ReceiveEndpointReady ready)
        {
            if (IsTarget(ready.InputAddress))
                _ready.TrySetResult(ready);

            return Task.CompletedTask;
        }

        public Task StoppingAsync(ReceiveEndpointStopping stopping) => Task.CompletedTask;

        public Task CompletedAsync(ReceiveEndpointCompleted completed) => Task.CompletedTask;

        public Task FaultedAsync(ReceiveEndpointFaulted faulted)
        {
            if (IsTarget(faulted.InputAddress))
                _faulted.TrySetResult(faulted);

            return Task.CompletedTask;
        }

        private bool IsTarget(Uri address)
        {
            string[] segments = address.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments.Length > 0
                && string.Equals(Uri.UnescapeDataString(segments[^1]), endpointName, StringComparison.Ordinal);
        }
    }

    private sealed class GatedReadyObserver(string endpointName) : IReceiveEndpointObserver
    {
        private readonly TaskCompletionSource<IReceiveEndpoint> _entered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _completed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _readyCount;

        public Task<IReceiveEndpoint> Entered => _entered.Task;
        public Task CompletedObserved => _completed.Task;
        public int ReadyCount => Volatile.Read(ref _readyCount);

        public async Task ReadyAsync(ReceiveEndpointReady ready)
        {
            if (!IsTarget(ready.InputAddress))
                return;

            Interlocked.Increment(ref _readyCount);
            _entered.TrySetResult(ready.ReceiveEndpoint);
            await _release.Task.ConfigureAwait(false);
        }

        public void Release() => _release.TrySetResult();

        public Task StoppingAsync(ReceiveEndpointStopping stopping) => Task.CompletedTask;

        public Task CompletedAsync(ReceiveEndpointCompleted completed)
        {
            if (IsTarget(completed.InputAddress))
                _completed.TrySetResult();

            return Task.CompletedTask;
        }

        public Task FaultedAsync(ReceiveEndpointFaulted faulted) => Task.CompletedTask;

        private bool IsTarget(Uri address)
        {
            string[] segments = address.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments.Length > 0
                && string.Equals(Uri.UnescapeDataString(segments[^1]), endpointName, StringComparison.Ordinal);
        }
    }

    private sealed class GatedBusStartObserver : IBusObserver
    {
        private readonly TaskCompletionSource _entered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _preStartCount;

        public Task Entered => _entered.Task;
        public int PreStartCount => Volatile.Read(ref _preStartCount);

        public void PostCreate(IBus bus)
        {
        }

        public void CreateFaulted(Exception exception)
        {
        }

        public Task PreStartAsync(IBus bus)
        {
            Interlocked.Increment(ref _preStartCount);
            _entered.TrySetResult();
            return _release.Task;
        }

        public Task PostStartAsync(IBus bus, Task<BusReady> busReady) => Task.CompletedTask;

        public Task StartFaultedAsync(IBus bus, Exception exception) => Task.CompletedTask;

        public Task PreStopAsync(IBus bus) => Task.CompletedTask;

        public Task PostStopAsync(IBus bus) => Task.CompletedTask;

        public Task StopFaultedAsync(IBus bus, Exception exception) => Task.CompletedTask;

        public void Release() => _release.TrySetResult();
    }

    private sealed class CancelAfterStartObserver(CancellationTokenSource cancellation) : IBusObserver
    {
        private int _postStopCount;
        private int _preStopCount;

        public int PostStopCount => Volatile.Read(ref _postStopCount);
        public int PreStopCount => Volatile.Read(ref _preStopCount);

        public void PostCreate(IBus bus)
        {
        }

        public void CreateFaulted(Exception exception)
        {
        }

        public Task PreStartAsync(IBus bus) => Task.CompletedTask;

        public Task PostStartAsync(IBus bus, Task<BusReady> busReady)
        {
            cancellation.Cancel();
            return Task.CompletedTask;
        }

        public Task StartFaultedAsync(IBus bus, Exception exception) => Task.CompletedTask;

        public Task PreStopAsync(IBus bus)
        {
            Interlocked.Increment(ref _preStopCount);
            return Task.CompletedTask;
        }

        public Task PostStopAsync(IBus bus)
        {
            Interlocked.Increment(ref _postStopCount);
            return Task.CompletedTask;
        }

        public Task StopFaultedAsync(IBus bus, Exception exception) => Task.CompletedTask;
    }

    private sealed class GatedFirstBusStopObserver : IBusObserver
    {
        private readonly TaskCompletionSource _entered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _postStopCount;
        private int _preStopCount;

        public Task Entered => _entered.Task;
        public int PostStopCount => Volatile.Read(ref _postStopCount);
        public int PreStopCount => Volatile.Read(ref _preStopCount);

        public void PostCreate(IBus bus)
        {
        }

        public void CreateFaulted(Exception exception)
        {
        }

        public Task PreStartAsync(IBus bus) => Task.CompletedTask;

        public Task PostStartAsync(IBus bus, Task<BusReady> busReady) => Task.CompletedTask;

        public Task StartFaultedAsync(IBus bus, Exception exception) => Task.CompletedTask;

        public Task PreStopAsync(IBus bus)
        {
            if (Interlocked.Increment(ref _preStopCount) != 1)
                return Task.CompletedTask;

            _entered.TrySetResult();
            return _release.Task;
        }

        public Task PostStopAsync(IBus bus)
        {
            Interlocked.Increment(ref _postStopCount);
            return Task.CompletedTask;
        }

        public Task StopFaultedAsync(IBus bus, Exception exception) => Task.CompletedTask;

        public void Release() => _release.TrySetResult();
    }

    private sealed class FaultingFirstPostStopObserver : IBusObserver
    {
        private int _postStopCount;
        private int _stopFaultedCount;

        public ExpectedStopException ExpectedFailure { get; } = new();
        public Exception? ObservedFailure { get; private set; }
        public int PostStopCount => Volatile.Read(ref _postStopCount);
        public int StopFaultedCount => Volatile.Read(ref _stopFaultedCount);

        public void PostCreate(IBus bus)
        {
        }

        public void CreateFaulted(Exception exception)
        {
        }

        public Task PreStartAsync(IBus bus) => Task.CompletedTask;

        public Task PostStartAsync(IBus bus, Task<BusReady> busReady) => Task.CompletedTask;

        public Task StartFaultedAsync(IBus bus, Exception exception) => Task.CompletedTask;

        public Task PreStopAsync(IBus bus) => Task.CompletedTask;

        public Task PostStopAsync(IBus bus)
        {
            return Interlocked.Increment(ref _postStopCount) == 1
                ? Task.FromException(ExpectedFailure)
                : Task.CompletedTask;
        }

        public Task StopFaultedAsync(IBus bus, Exception exception)
        {
            Interlocked.Increment(ref _stopFaultedCount);
            ObservedFailure = exception;
            return Task.FromException(new ExpectedStopFaultObserverException());
        }
    }

    private sealed class FaultingFirstPreStartObserver : IBusObserver
    {
        private int _preStartCount;
        private int _startFaultedCount;

        public ExpectedStartObserverException ExpectedFailure { get; } = new();
        public Exception? ObservedFailure { get; private set; }
        public int PreStartCount => Volatile.Read(ref _preStartCount);
        public int StartFaultedCount => Volatile.Read(ref _startFaultedCount);

        public void PostCreate(IBus bus)
        {
        }

        public void CreateFaulted(Exception exception)
        {
        }

        public Task PreStartAsync(IBus bus)
        {
            return Interlocked.Increment(ref _preStartCount) == 1
                ? Task.FromException(ExpectedFailure)
                : Task.CompletedTask;
        }

        public Task PostStartAsync(IBus bus, Task<BusReady> busReady) => Task.CompletedTask;

        public Task StartFaultedAsync(IBus bus, Exception exception)
        {
            Interlocked.Increment(ref _startFaultedCount);
            ObservedFailure = exception;
            return Task.FromException(new ExpectedStartFaultObserverException());
        }

        public Task PreStopAsync(IBus bus) => Task.CompletedTask;

        public Task PostStopAsync(IBus bus) => Task.CompletedTask;

        public Task StopFaultedAsync(IBus bus, Exception exception) => Task.CompletedTask;
    }

    private sealed class ExpectedStartupException : Exception;
    private sealed class ExpectedStartObserverException : Exception;
    private sealed class ExpectedStartFaultObserverException : Exception;
    private sealed class ExpectedStopException : Exception;
    private sealed class ExpectedStopFaultObserverException : Exception;

    public sealed record LifecycleRequest(Guid Id, int Run);
    public sealed record LifecycleResponse(Guid Id, int Run);
    public sealed record BlockedMessage;
}
