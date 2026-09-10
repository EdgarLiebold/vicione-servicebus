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

        await harness.RestartHostedServicesAsync(TestContext.Current.CancellationToken);

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
}
