using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class TestingServiceProviderExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TESTING-SERVICE-PROVIDER", "filtered-publish-handler")]
    public async Task ConnectPublishHandler_ReturnsOnlyTheExactMatchingPublishedContextAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration => configuration.SetTestTimeouts(timeout, timeout))
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid expectedId = NewId.NextGuid();
            await using IPublishMessageObservation<PublishedMessage> observation =
                await harness.ObservePublishedMessageAsync<PublishedMessage>(
                    context => context.Message.CorrelationId == expectedId,
                    cancellationToken);

            await harness.Bus.PublishAsync(new PublishedMessage(NewId.NextGuid(), "rejected"), cancellationToken);
            await harness.Bus.PublishAsync(new PublishedMessage(expectedId, "expected"), cancellationToken);

            ConsumeContext<PublishedMessage> context = await observation.Message.WaitAsync(timeout, cancellationToken);
            Assert.Equal(expectedId, context.Message.CorrelationId);
            Assert.Equal("expected", context.Message.Value);
            Assert.Equal(context.Message.CorrelationId, context.CorrelationId);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TESTING-SERVICE-PROVIDER", "harness-bound-task-registrations")]
    public async Task RegisteredTaskCompletionSources_PreserveIdentityOrderAndIndependentCompletionAsync()
    {
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.AddTaskCompletionSource<string>();
                configuration.AddTaskCompletionSource<string>();
            })
            .BuildServiceProvider(validateScopes: true);

        TaskCompletionSource<string>[] sources = provider.GetServices<TaskCompletionSource<string>>().ToArray();
        Task<string>[] tasks = provider.GetTasks<string>();
        Task<string> required = provider.GetTaskAsync<string>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, sources.Length);
        Assert.Equal(2, tasks.Length);
        Assert.Same(sources[0].Task, tasks[0]);
        Assert.Same(sources[1].Task, tasks[1]);
        Assert.NotSame(tasks[1], required);
        Assert.False(tasks[0].IsCompleted);
        Assert.False(tasks[1].IsCompleted);

        sources[1].SetResult("second");
        Assert.Equal("second", await tasks[1]);
        Assert.Equal("second", await required);
        Assert.False(tasks[0].IsCompleted);

        sources[0].SetResult("first");
        Assert.Equal("first", await tasks[0]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TESTING-SERVICE-PROVIDER", "publish-handler-ready-timeout")]
    public async Task ConnectPublishHandler_TimesOutOnTheHarnessClockWhenTheEndpointCannotBecomeReadyAsync()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero));
        var endpointHandle = new PendingEndpointHandle();
        IBus bus = DispatchProxy.Create<IBus, PendingBusProxy>();
        ((PendingBusProxy)(object)bus).EndpointHandle = endpointHandle;
        ITestHarness harness = DispatchProxy.Create<ITestHarness, PendingHarnessProxy>();
        ((PendingHarnessProxy)(object)harness).Configure(bus, timeProvider, TimeSpan.FromSeconds(1));

        Task<IPublishMessageObservation<PublishedMessage>> connection =
            harness.ObservePublishedMessageAsync<PublishedMessage>(
                _ => true,
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(connection.IsCompleted);

        timeProvider.Advance(TimeSpan.FromSeconds(1));
        await Task.Yield();
        await Task.Yield();

        Assert.True(connection.IsCompleted);
        await Assert.ThrowsAsync<TimeoutException>(() => connection);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TESTING-SERVICE-PROVIDER", "publish-handler-required-dependencies")]
    public async Task ConnectPublishHandler_RejectsMissingRequiredDependenciesBeforeEndpointCreationAsync()
    {
        ArgumentNullException missingHarness = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            TestingServiceProviderExtensions.ObservePublishedMessageAsync<PublishedMessage>(
                null!,
                _ => true,
                TestContext.Current.CancellationToken));
        Assert.Equal("harness", missingHarness.ParamName);

        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness()
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = provider.GetTestHarness();

        ArgumentNullException missingFilter = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            harness.ObservePublishedMessageAsync<PublishedMessage>(
                null!,
                cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal("filter", missingFilter.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TESTING-SERVICE-PROVIDER", "harness-bound-task-caller-cancellation")]
    public async Task GetTaskAsync_CallerCancellationEndsOnlyTheWaitAndPreservesTheExactTokenAsync()
    {
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration => configuration.AddTaskCompletionSource<string>())
            .BuildServiceProvider(validateScopes: true);
        TaskCompletionSource<string> source = provider.GetRequiredService<TaskCompletionSource<string>>();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        Task<string> wait = provider.GetTaskAsync<string>(cancellation.Token);
        cancellation.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.False(source.Task.IsCompleted);

        source.SetResult("still-usable");
        Assert.Equal("still-usable", await source.Task);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TESTING-SERVICE-PROVIDER", "required-provider-input")]
    public void TaskResolution_RejectsANullProvider()
    {
        Assert.Equal(
            "provider",
            Assert.Throws<ArgumentNullException>(() => ServiceProviderTestExtensions.GetTasks<string>(null!)).ParamName);
        Assert.Equal(
            "provider",
            Assert.Throws<ArgumentNullException>(() =>
            {
                _ = ServiceProviderTestExtensions.GetTaskAsync<string>(
                    null!,
                    TestContext.Current.CancellationToken);
            }).ParamName);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record PublishedMessage(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed class PendingEndpointHandle : IHostReceiveEndpointHandle
    {
        private readonly TaskCompletionSource<ReceiveEndpointReady> _ready =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IReceiveEndpoint ReceiveEndpoint => throw new NotSupportedException();

        public Task<ReceiveEndpointReady> Ready => _ready.Task;

        public Task StopAsync(CancellationToken cancellationToken = default) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask; }
    }

    private class PendingBusProxy : DispatchProxy
    {
        public IHostReceiveEndpointHandle? EndpointHandle { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IReceiveConnector.ConnectReceiveEndpoint)
                && targetMethod.ReturnType == typeof(IHostReceiveEndpointHandle))
                return EndpointHandle ?? throw new InvalidOperationException("The endpoint handle was not configured.");

            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    private class PendingHarnessProxy : DispatchProxy
    {
        private IBus? _bus;
        private TimeProvider? _timeProvider;
        private TimeSpan _timeout;

        public void Configure(IBus bus, TimeProvider timeProvider, TimeSpan timeout)
        {
            _bus = bus;
            _timeProvider = timeProvider;
            _timeout = timeout;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_Bus" => _bus ?? throw new InvalidOperationException("The bus was not configured."),
                "get_TestTimeout" => _timeout,
                "get_TimeProvider" => _timeProvider ?? throw new InvalidOperationException("The time provider was not configured."),
                "get_CancellationToken" => CancellationToken.None,
                "GetTask" => Activator.CreateInstance(
                    targetMethod.ReturnType,
                    TaskCreationOptions.RunContinuationsAsynchronously),
                _ => throw new NotSupportedException(targetMethod?.Name),
            };
        }
    }
}
