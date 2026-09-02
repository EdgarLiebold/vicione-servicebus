using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
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
    [RequirementCoverage("REQ-VSB-INMEMORY-LIFECYCLE", "stop-awaits-owned-startup-observation")]
    public async Task Stop_WaitsForTheOwnedStartupObservationBeforeCompleting()
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
        Task stop = transportHandle.Stop(CancellationToken.None);

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
    public async Task StartupDependencyFault_IsPublishedAsTerminalAndTheBusStillStops()
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

        await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);
        dependency.Fail(expected);
        ReceiveEndpointFaulted fault = await observer.FaultObserved.WaitAsync(timeout, cancellationToken);
        await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);

        Assert.True(fault.IsTerminal);
        Assert.Same(expected, fault.Exception);
        Assert.False(observer.ReadyObserved.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-LIFECYCLE", "start-request-stop-and-restart")]
    public async Task StartRequestStopAndRestart_PreservesExactRequestRoutingAcrossBothRuns()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration =>
            configuration.ReceiveEndpoint($"lifecycle-{NewId.NextGuid():N}", endpoint =>
                endpoint.Handler<LifecycleRequest>(context =>
                    context.RespondAsync(new LifecycleResponse(context.Message.Id, context.Message.Run)))));

        LifecycleResponse first = await RunOnce(bus, new LifecycleRequest(NewId.NextGuid(), 1), timeout, cancellationToken);
        LifecycleResponse second = await RunOnce(bus, new LifecycleRequest(NewId.NextGuid(), 2), timeout, cancellationToken);

        Assert.Equal(1, first.Run);
        Assert.Equal(2, second.Run);
        Assert.NotEqual(first.Id, second.Id);
    }

    private static async Task<LifecycleResponse> RunOnce(
        IBusControl bus,
        LifecycleRequest request,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        await bus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Response<LifecycleResponse> response = await bus.CreateRequestClient<LifecycleRequest>()
                .GetResponse<LifecycleResponse>(request, cancellationToken)
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

    public sealed class FailingDependency : IReceiveEndpointDependency
    {
        private readonly TaskCompletionSource _ready =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Ready => _ready.Task;

        public void Fail(Exception exception) => _ready.TrySetException(exception);
    }

    public sealed class BlockedConsumer : IConsumer<BlockedMessage>
    {
        public Task Consume(ConsumeContext<BlockedMessage> context) => Task.CompletedTask;
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

        public Task Ready(ReceiveEndpointReady ready)
        {
            if (IsTarget(ready.InputAddress))
                _ready.TrySetResult(ready);

            return Task.CompletedTask;
        }

        public Task Stopping(ReceiveEndpointStopping stopping) => Task.CompletedTask;

        public Task Completed(ReceiveEndpointCompleted completed) => Task.CompletedTask;

        public Task Faulted(ReceiveEndpointFaulted faulted)
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

        public async Task Ready(ReceiveEndpointReady ready)
        {
            if (!IsTarget(ready.InputAddress))
                return;

            Interlocked.Increment(ref _readyCount);
            _entered.TrySetResult(ready.ReceiveEndpoint);
            await _release.Task.ConfigureAwait(false);
        }

        public void Release() => _release.TrySetResult();

        public Task Stopping(ReceiveEndpointStopping stopping) => Task.CompletedTask;

        public Task Completed(ReceiveEndpointCompleted completed)
        {
            if (IsTarget(completed.InputAddress))
                _completed.TrySetResult();

            return Task.CompletedTask;
        }

        public Task Faulted(ReceiveEndpointFaulted faulted) => Task.CompletedTask;

        private bool IsTarget(Uri address)
        {
            string[] segments = address.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments.Length > 0
                && string.Equals(Uri.UnescapeDataString(segments[^1]), endpointName, StringComparison.Ordinal);
        }
    }

    private sealed class ExpectedStartupException : Exception;

    public sealed record LifecycleRequest(Guid Id, int Run);
    public sealed record LifecycleResponse(Guid Id, int Run);
    public sealed record BlockedMessage;
}
