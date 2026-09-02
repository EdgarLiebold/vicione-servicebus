using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.DependencyInjection.Testing;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class HostedServiceLifecycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "hosted-services-restart-order")]
    public async Task RestartHostedServices_StopsInReverseOrderAndStartsInRegistrationOrder()
    {
        var calls = new List<string>();
        await using ServiceProvider provider = CreateProvider(calls);
        await using var harness = CreateHarness(provider);

        await harness.Start();
        calls.Clear();

        await harness.RestartHostedServices(TestContext.Current.CancellationToken);

        Assert.Equal(
            ["stop:third", "stop:second", "stop:first", "start:first", "start:second", "start:third"],
            calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "dispose-reverse-order-and-idempotence")]
    public async Task DisposeAsync_StopsHostedServicesInReverseOrderExactlyOnce()
    {
        var calls = new List<string>();
        await using ServiceProvider provider = CreateProvider(calls);
        var harness = CreateHarness(provider);
        await harness.Start();
        calls.Clear();

        await harness.DisposeAsync();
        await harness.DisposeAsync();

        Assert.Equal(["stop:third", "stop:second", "stop:first"], calls);
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
            calls.Add($"start:{name}");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            calls.Add($"stop:{name}");
            return Task.CompletedTask;
        }
    }
}
