using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Testing.Internal;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class HostedServiceLifecycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "hosted-services-restart-order")]
    public async Task RestartHostedServices_StopsInReverseOrderAndStartsInRegistrationOrderAsync()
    {
        var calls = new List<string>();
        await using ServiceProvider provider = CreateProvider(calls);
        await using var harness = CreateHarness(provider);

        await harness.StartAsync(TestContext.Current.CancellationToken);
        calls.Clear();

        await harness.RestartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            ["stop:third", "stop:second", "stop:first", "start:first", "start:second", "start:third"],
            calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "dispose-reverse-order-and-idempotence")]
    public async Task DisposeAsync_StopsHostedServicesInReverseOrderExactlyOnceAsync()
    {
        var calls = new List<string>();
        await using ServiceProvider provider = CreateProvider(calls);
        var harness = CreateHarness(provider);
        await harness.StartAsync(TestContext.Current.CancellationToken);
        calls.Clear();

        await harness.DisposeAsync();
        await harness.DisposeAsync();

        Assert.Equal(["stop:third", "stop:second", "stop:first"], calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "caller-start-token")]
    public async Task StartAsync_ForwardsTheExactCallerTokenToEveryHostedServiceAsync()
    {
        var tokens = new List<CancellationToken>();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IHostedService>(new TokenRecordingHostedService(tokens))
            .BuildServiceProvider(validateScopes: true);
        await using var harness = CreateHarness(provider);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        await harness.StartAsync(cancellation.Token);

        Assert.Equal([cancellation.Token], tokens);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "default-start-token-uses-test-budget")]
    public async Task StartAsync_UsesTheHarnessBudgetWhenTheCallerDoesNotSupplyATokenAsync()
    {
        var tokens = new List<CancellationToken>();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IHostedService>(new TokenRecordingHostedService(tokens))
            .BuildServiceProvider(validateScopes: true);
        await using var harness = CreateHarness(provider);

        await StartWithHarnessBudgetAsync(harness);

        CancellationToken token = Assert.Single(tokens);
        Assert.True(token.CanBeCanceled);
        Assert.Equal(harness.CancellationToken, token);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "cancel-materializes-container-scope")]
    public async Task Cancel_CancelsTheContainerScopeBeforeItsTokenIsFirstRequestedAsync()
    {
        using ServiceProvider provider = new ServiceCollection().BuildServiceProvider(validateScopes: true);
        var harness = CreateHarness(provider);

        harness.Cancel();

        Assert.True(harness.CancellationToken.IsCancellationRequested);
        await harness.DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "container-completion-observes-scope-and-caller-cancellation")]
    public async Task CompletionSource_ObservesBothContainerScopeAndCallerCancellationAsync()
    {
        await using ServiceProvider provider = new ServiceCollection().BuildServiceProvider(validateScopes: true);
        await using var harness = CreateHarness(provider);
        using var callerCancellation = new CancellationTokenSource();
        Task<int> callerTask = harness.CreateTaskCompletionSource<int>(callerCancellation.Token).Task;

        callerCancellation.Cancel();

        OperationCanceledException callerException = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => callerTask);
        Assert.Equal(callerCancellation.Token, callerException.CancellationToken);

        Task<int> harnessTask = harness.CreateTaskCompletionSource<int>(TestContext.Current.CancellationToken).Task;
        CancellationToken harnessToken = harness.CancellationToken;

        harness.Cancel();

        OperationCanceledException harnessException = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => harnessTask);
        Assert.Equal(harnessToken, harnessException.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "explicit-stop-prevents-disposal-stop")]
    public async Task StopAsync_StopsEachServiceOnceAndPreventsASecondStopDuringDisposalAsync()
    {
        var calls = new List<string>();
        await using ServiceProvider provider = CreateProvider(calls);
        var harness = CreateHarness(provider);
        await harness.StartAsync(TestContext.Current.CancellationToken);
        calls.Clear();

        await harness.StopAsync(TestContext.Current.CancellationToken);
        await harness.DisposeAsync();

        Assert.Equal(["stop:third", "stop:second", "stop:first"], calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "stop-attempts-every-service")]
    public async Task StopAsync_AttemptsEveryServiceAndPropagatesTheExactSingleFailureAsync()
    {
        var calls = new List<string>();
        var expected = new InvalidOperationException("third failed to stop");
        var failing = new ToggleFailingStopHostedService("third", calls, expected);
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IHostedService>(new RecordingHostedService("first", calls))
            .AddSingleton<IHostedService>(new RecordingHostedService("second", calls))
            .AddSingleton<IHostedService>(failing)
            .BuildServiceProvider(validateScopes: true);
        var harness = CreateHarness(provider);
        await harness.StartAsync(TestContext.Current.CancellationToken);
        calls.Clear();

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.StopAsync(TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(["stop:third", "stop:second", "stop:first"], calls);

        failing.FailOnStop = false;
        await harness.DisposeAsync();
        Assert.Equal("stop:third", calls[^1]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "restart-start-failure-rolls-back")]
    public async Task RestartAsync_RollsBackOnlyServicesStartedByTheFailedRestartAsync()
    {
        var calls = new List<string>();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IHostedService>(new RecordingHostedService("first", calls))
            .AddSingleton<IHostedService>(new FailOnSecondStartHostedService("second", calls))
            .AddSingleton<IHostedService>(new RecordingHostedService("third", calls))
            .BuildServiceProvider(validateScopes: true);
        await using var harness = CreateHarness(provider);
        await harness.StartAsync(TestContext.Current.CancellationToken);
        calls.Clear();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.RestartAsync(TestContext.Current.CancellationToken));

        Assert.Equal("second restart failed", exception.Message);
        Assert.Equal(
            ["stop:third", "stop:second", "stop:first", "start:first", "start:second", "stop:first"],
            calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "cleanup-independent-from-harness-timeout")]
    public async Task DisposeAsync_StopsServicesAfterTheHarnessTimeoutTokenWasCanceledAsync()
    {
        var stopTokens = new List<CancellationToken>();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IHostedService>(new StopTokenRecordingHostedService(stopTokens))
            .BuildServiceProvider(validateScopes: true);
        var harness = CreateHarness(provider);
        await harness.StartAsync(TestContext.Current.CancellationToken);
        _ = harness.CancellationToken;
        harness.Cancel();

        await harness.DisposeAsync();

        CancellationToken stopToken = Assert.Single(stopTokens);
        Assert.False(stopToken.CanBeCanceled);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "partial-start-rollback")]
    public async Task StartFailure_StopsOnlyStartedServicesInReverseOrderAsync()
    {
        var calls = new List<string>();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IHostedService>(new RecordingHostedService("first", calls))
            .AddSingleton<IHostedService>(new FailingStartHostedService("second", calls))
            .AddSingleton<IHostedService>(new RecordingHostedService("third", calls))
            .BuildServiceProvider(validateScopes: true);
        await using var harness = CreateHarness(provider);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.StartAsync(TestContext.Current.CancellationToken));

        Assert.Equal("second failed", exception.Message);
        Assert.Equal(["start:first", "start:second", "stop:first"], calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "required-constructor-inputs")]
    public void Constructor_RejectsEachMissingRequiredDependency()
    {
        using ServiceProvider provider = new ServiceCollection().BuildServiceProvider(validateScopes: true);
        IOptions<TestHarnessOptions> options = Options.Create(new TestHarnessOptions());

        Assert.Equal(
            "provider",
            Assert.Throws<ArgumentNullException>(() => new ContainerTestHarness(null!, options, TimeProvider.System)).ParamName);
        Assert.Equal(
            "options",
            Assert.Throws<ArgumentNullException>(() => new ContainerTestHarness(provider, null!, TimeProvider.System)).ParamName);
        Assert.Equal(
            "timeProvider",
            Assert.Throws<ArgumentNullException>(() => new ContainerTestHarness(provider, options, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "observer-faults-preserve-primary-failure")]
    public async Task BusObserver_FaultNotificationsNeverMaskThePrimaryLifecycleFailureAsync()
    {
        await using ServiceProvider provider = new ServiceCollection().BuildServiceProvider(validateScopes: true);
        await using var harness = CreateHarness(provider);
        var observer = new ContainerTestHarnessBusObserver(harness);
        var expected = new InvalidOperationException("primary lifecycle failure");

        observer.CreateFaulted(expected);
        Task startFault = observer.StartFaultedAsync(null!, expected);
        Task stopFault = observer.StopFaultedAsync(null!, expected);
        await Task.WhenAll(startFault, stopFault);

        Assert.True(startFault.IsCompletedSuccessfully);
        Assert.True(stopFault.IsCompletedSuccessfully);
    }

    private static Task StartWithHarnessBudgetAsync(ContainerTestHarness harness) => harness.StartAsync();

    private static ServiceProvider CreateProvider(List<string> calls) =>
        new ServiceCollection()
            .AddSingleton<IHostedService>(new RecordingHostedService("first", calls))
            .AddSingleton<IHostedService>(new RecordingHostedService("second", calls))
            .AddSingleton<IHostedService>(new RecordingHostedService("third", calls))
            .BuildServiceProvider(validateScopes: true);

    private static ContainerTestHarness CreateHarness(IServiceProvider provider) =>
        new(provider, Options.Create(new TestHarnessOptions()), TimeProvider.System);

    private sealed class RecordingHostedService(string name, List<string> calls) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            calls.Add($"start:{name}");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            calls.Add($"stop:{name}");
            return Task.CompletedTask;
        }
    }

    private sealed class TokenRecordingHostedService(List<CancellationToken> tokens) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            tokens.Add(cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StopTokenRecordingHostedService(List<CancellationToken> tokens) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken)
        {
            tokens.Add(cancellationToken);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingStartHostedService(string name, List<string> calls) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            calls.Add($"start:{name}");
            return Task.FromException(new InvalidOperationException($"{name} failed"));
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            calls.Add($"stop:{name}");
            return Task.CompletedTask;
        }
    }

    private sealed class ToggleFailingStopHostedService(
        string name,
        List<string> calls,
        Exception failure) : IHostedService
    {
        public bool FailOnStop { get; set; } = true;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            calls.Add($"start:{name}");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            calls.Add($"stop:{name}");
            return FailOnStop ? Task.FromException(failure) : Task.CompletedTask;
        }
    }

    private sealed class FailOnSecondStartHostedService(string name, List<string> calls) : IHostedService
    {
        private int _startCount;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            calls.Add($"start:{name}");
            return ++_startCount == 2
                ? Task.FromException(new InvalidOperationException($"{name} restart failed"))
                : Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            calls.Add($"stop:{name}");
            return Task.CompletedTask;
        }
    }
}
