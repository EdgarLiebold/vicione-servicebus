using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.EventHubs.Testing;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class EventHubProducerResolutionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-PRODUCER-RESOLUTION", "public-boundaries-reject-missing-required-input")]
    public async Task ProducerResolution_RejectsMissingRequiredInputAsync()
    {
        IEventHubProducerProvider provider = DispatchProxy.Create<IEventHubProducerProvider, UnexpectedCallProxy>();
        ITestHarness harness = DispatchProxy.Create<ITestHarness, UnexpectedCallProxy>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        ArgumentNullException missingProvider = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            EventHubProducerExtensions.GetProducerAsync(null!, "orders", cancellationToken));
        ArgumentNullException missingHarness = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            EventHubTestHarnessExtensions.GetProducerAsync(null!, "orders", cancellationToken));

        Assert.Equal("producerProvider", missingProvider.ParamName);
        Assert.Equal("harness", missingHarness.ParamName);

        foreach (string? eventHubName in new[] { null, string.Empty, " " })
        {
            ArgumentException providerName = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
                provider.GetProducerAsync(eventHubName!, cancellationToken));
            ArgumentException harnessName = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
                harness.GetProducerAsync(eventHubName!, cancellationToken));

            Assert.Equal("eventHubName", providerName.ParamName);
            Assert.Equal("eventHubName", harnessName.ParamName);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-PRODUCER-RESOLUTION", "entity-name-and-cancellation-forwarded-to-provider")]
    public async Task ProviderExtension_ForwardsEntityAddressAndCancellationAsync()
    {
        IEventHubProducer producer = DispatchProxy.Create<IEventHubProducer, UnexpectedCallProxy>();
        var provider = new RecordingProducerProvider(producer);
        CancellationToken cancellationToken = new(canceled: true);

        IEventHubProducer actual = await provider.GetProducerAsync("orders", cancellationToken);

        Assert.Same(producer, actual);
        Assert.Equal(new Uri("topic:orders"), provider.Address);
        Assert.Equal(cancellationToken, provider.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-PRODUCER-RESOLUTION", "harness-scope-resolves-provider-and-forwards-input")]
    public async Task HarnessExtension_ResolvesProviderFromScopeAndForwardsInputAsync()
    {
        IEventHubProducer producer = DispatchProxy.Create<IEventHubProducer, UnexpectedCallProxy>();
        var provider = new RecordingProducerProvider(producer);
        await using ServiceProvider services = new ServiceCollection()
            .AddSingleton<IEventHubProducerProvider>(provider)
            .BuildServiceProvider(true);
        using IServiceScope scope = services.CreateScope();
        ITestHarness harness = CreateHarness(scope);
        CancellationToken cancellationToken = new(canceled: true);

        IEventHubProducer actual = await harness.GetProducerAsync("orders", cancellationToken);

        Assert.Same(producer, actual);
        Assert.Equal(new Uri("topic:orders"), provider.Address);
        Assert.Equal(cancellationToken, provider.CancellationToken);
    }

    static ITestHarness CreateHarness(IServiceScope scope)
    {
        ITestHarness harness = DispatchProxy.Create<ITestHarness, HarnessProxy>();
        ((HarnessProxy)(object)harness).Scope = scope;
        return harness;
    }

    class HarnessProxy : DispatchProxy
    {
        internal IServiceScope Scope { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_Scope")
                return Scope;

            throw new InvalidOperationException($"Unexpected harness call: {targetMethod?.Name}.");
        }
    }

    class UnexpectedCallProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"Unexpected call: {targetMethod?.Name}.");
    }

    sealed class RecordingProducerProvider : IEventHubProducerProvider
    {
        readonly IEventHubProducer _producer;

        internal RecordingProducerProvider(IEventHubProducer producer)
        {
            _producer = producer;
        }

        internal Uri? Address { get; private set; }

        internal CancellationToken CancellationToken { get; private set; }

        public Task<IEventHubProducer> GetProducerAsync(Uri address, CancellationToken cancellationToken = default)
        {
            Address = address;
            CancellationToken = cancellationToken;
            return Task.FromResult(_producer);
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => throw new NotSupportedException();
    }
}
