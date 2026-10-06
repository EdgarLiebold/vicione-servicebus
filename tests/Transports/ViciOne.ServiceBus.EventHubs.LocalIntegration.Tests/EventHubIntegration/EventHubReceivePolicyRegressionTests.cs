using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.EventHubs.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

// Public builder SPI observes one build generation without private context reflection.
public sealed class EventHubReceivePolicyRegressionTests
{
    private static TimeSpan Timeout => TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
    private const long MaximumTimerMilliseconds = 4294967294L;

    [Theory]
    [InlineData("count-zero", "CheckpointMessageCount")]
    [InlineData("limit-zero", "CheckpointMessageLimit")]
    [InlineData("interval-zero", "CheckpointInterval")]
    [InlineData("interval-negative", "CheckpointInterval")]
    [InlineData("interval-infinite", "CheckpointInterval")]
    [InlineData("interval-overflow", "CheckpointInterval")]
    [InlineData("delivery-zero", "ConcurrentDeliveryLimit")]
    [InlineData("delivery-negative", "ConcurrentDeliveryLimit")]
    [InlineData("prefetch-zero", "PrefetchCount")]
    [RequirementCoverage("REQ-VSB-EVENTHUB-CONFIGURATION-OWNERSHIP", "invalid-receive-policy-fails-ordinary-build-before-sdk-start")]
    public async Task InvalidReceivePolicy_FailsOrdinaryBusBuildWithActionableProviderDiagnosticAsync(string mode, string property)
    {
        int optionsCalls = 0;
        await using ServiceProvider provider = CreateProvider(true, endpoint =>
        {
            endpoint.ConfigureOptions = _ => Interlocked.Increment(ref optionsCalls);
            ApplyInvalid(endpoint, mode);
        });
        ConfigurationException error = await Assert.ThrowsAsync<ConfigurationException>(() => ResolveAndValidateAsync(provider));
        string diagnostic = Diagnostic(error);
        Assert.Contains(property, diagnostic, StringComparison.Ordinal);
        Assert.Contains("Event Hubs", diagnostic, StringComparison.Ordinal);
        Assert.Contains("bus '", diagnostic, StringComparison.Ordinal);
        Assert.Contains("Set " + property, diagnostic, StringComparison.Ordinal);
        Assert.Equal(0, optionsCalls);
    }

    [Theory]
    [InlineData("prefetch-negative", "PrefetchCount", "Must be >= 0")]
    [InlineData("concurrent-zero", "ConcurrentMessageLimit", "Must be > 0")]
    [InlineData("concurrent-negative", "ConcurrentMessageLimit", "Must be > 0")]
    [RequirementCoverage("REQ-VSB-EVENTHUB-CONFIGURATION-OWNERSHIP", "provider-validation-preserves-inherited-negative-transport-guards")]
    public async Task InheritedTransportGuards_StillRejectTheirInvalidLimitsAsync(string mode, string property, string message)
    {
        await using ServiceProvider provider = CreateProvider(true, endpoint => ApplyInvalid(endpoint, mode));
        ConfigurationException error = await Assert.ThrowsAsync<ConfigurationException>(() => ResolveAndValidateAsync(provider));
        string diagnostic = Diagnostic(error);
        Assert.Contains(property, diagnostic, StringComparison.Ordinal);
        Assert.Contains(message, diagnostic, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(10000L)]
    [InlineData(42949672940000L)]
    [InlineData(42949672940001L)]
    [InlineData(42949672949999L)]
    [RequirementCoverage("REQ-VSB-EVENTHUB-CONFIGURATION-OWNERSHIP", "positive-interval-accepts-native-whole-millisecond-timer-boundary")]
    public async Task ValidTimerBoundary_PreservesWholeMillisecondSemanticsAsync(long ticks)
    {
        TimeSpan interval = TimeSpan.FromTicks(ticks);
        Assert.True(interval > TimeSpan.Zero);
        Assert.True((long)interval.TotalMilliseconds <= MaximumTimerMilliseconds);
        using var nativeTimer = new CancellationTokenSource(interval); // Independent native timer boundary control.
        await using ServiceProvider provider = CreateProvider(true, endpoint => endpoint.CheckpointInterval = interval);
        await ResolveAndValidateAsync(provider);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EVENTHUB-CONFIGURATION-OWNERSHIP", "valid-selected-and-unselected-provider-build-without-sdk-activation")]
    public async Task ValidConfiguration_BuildsWithOrWithoutOptionalEventHubSelectionAsync(bool selected)
    {
        int optionsCalls = 0;
        await using ServiceProvider provider = CreateProvider(selected, endpoint => endpoint.ConfigureOptions = _ => optionsCalls++);
        await ResolveAndValidateAsync(provider);
        Assert.Equal(0, optionsCalls);
    }

    [Theory]
    [InlineData("container")]
    [InlineData("count")]
    [InlineData("limit")]
    [InlineData("prefetch")]
    [InlineData("interval")]
    [InlineData("concurrent")]
    [InlineData("delivery")]
    [RequirementCoverage("REQ-VSB-EVENTHUB-CONFIGURATION-OWNERSHIP", "built-generation-preserves-all-nine-settings-after-retained-configurator-mutation")]
    public async Task BuiltSettings_RemainOneImmutablePolicyAfterLaterConfigurationChangesAsync(string mode)
    {
        EventHubBuiltContextCapture? capture = null;
        EventHubReceiveEndpointConfigurator? retained = null;
        await using ServiceProvider provider = CreateProvider(true, endpoint =>
        {
            retained = Assert.IsType<EventHubReceiveEndpointConfigurator>(endpoint);
        }, value => capture = value);
        await ResolveAndValidateAsync(provider);
        Assert.NotNull(retained);
        Assert.NotNull(capture);
        Assert.Equal(1, capture.ContextCount);
        IEventHubReceiveEndpointContext context = capture.Context;
        Assert.Same(MessageLimits.Conservative, context.GetPayload<MessageLimits>());
        ReceiveSettings settings = context.GetPayload<ReceiveSettings>();
        Policy original = Capture(settings);
        Assert.Equal(new Policy("policy-eh", "policy-group", "policy-container", 2, 1, 1, TimeSpan.FromMinutes(1), 1, 1), original);
        Assert.NotSame(retained, settings);
        switch (mode)
        {
            case "container":
                retained.ContainerName = "later-container";
                Assert.Equal("later-container", retained.ContainerName);
                Assert.Contains(retained.Validate(), result => result.Key == nameof(EventHubReceiveEndpointConfigurator.ContainerName)
                    && result.Message == "was modified after being used");
                break;
            case "count": retained.CheckpointMessageCount = 2; break;
            case "limit": retained.CheckpointMessageLimit = 3; break;
            case "prefetch": retained.PrefetchCount = 2; break;
            case "interval": retained.CheckpointInterval = TimeSpan.FromMinutes(2); break;
            case "concurrent": retained.ConcurrentMessageLimit = 2; break;
            case "delivery": retained.ConcurrentDeliveryLimit = 2; break;
            default: throw new ArgumentOutOfRangeException(nameof(mode));
        }
        Assert.NotEqual(original, Capture(retained));
        Assert.Equal(original, Capture(settings));
        Assert.Same(settings, context.GetPayload<ReceiveSettings>());
        // Only the copied policy is observed here. Client-factory coherence is checked separately.
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EVENTHUB-CONFIGURATION-OWNERSHIP", "built-generation-keeps-original-options-before-and-after-callback-replacement")]
    public async Task BuiltClientFactory_UsesOriginalOptionsForCompletedGenerationAsync(bool replaceCallback)
    {
        EventHubBuiltContextCapture? capture = null;
        EventHubReceiveEndpointConfigurator? retained = null;
        int originalCalls = 0;
        int replacementCalls = 0;
        EventProcessorClientOptions? originalOptions = null;
        await using ServiceProvider provider = CreateProvider(true, endpoint =>
        {
            retained = Assert.IsType<EventHubReceiveEndpointConfigurator>(endpoint);
            endpoint.ConfigureOptions = options =>
            {
                originalCalls++;
                originalOptions = options;
                options.Identifier = "original-generation";
            };
        }, value => capture = value);
        await ResolveAndValidateAsync(provider);
        Assert.NotNull(retained);
        Assert.Equal(0, originalCalls);
        if (replaceCallback) retained.ConfigureOptions = options =>
        {
            replacementCalls++;
            options.Identifier = "replacement-generation";
        };
        Assert.NotNull(capture);
        Assert.Equal(1, capture.ContextCount);
        IEventHubReceiveEndpointContext context = capture.Context;
        Assert.Same(MessageLimits.Conservative, context.GetPayload<MessageLimits>());
        IProcessorContextSupervisor supervisor = context.ContextSupervisor;
        EventProcessorClient? client = null;
        try
        {
            await supervisor.SendAsync(Pipe.Execute<ProcessorContext>(processor =>
            {
                client = processor.GetClient(new EmptyBuilder());
                try
                {
                    Assert.Equal("original-generation", client.Identifier);
                    Assert.Equal(context.GetPayload<ReceiveSettings>().EventHubName, client.EventHubName);
                    Assert.Equal(context.GetPayload<ReceiveSettings>().ConsumerGroup, client.ConsumerGroup);
                    Assert.False(client.IsRunning);
                }
                finally { processor.ReleaseClient(); }
            }), TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Assert.Equal(1, originalCalls);
            Assert.Equal(0, replacementCalls);
            Assert.NotNull(originalOptions);
        }
        finally
        {
            try { await supervisor.StopAsync("research-policy-cleanup", CancellationToken.None).WaitAsync(Timeout, CancellationToken.None); }
            finally { if (client is not null) await client.StopProcessingAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None); }
        }
    }

    private static ServiceProvider CreateProvider(bool selected, Action<IEventHubReceiveEndpointConfigurator> configure,
        Action<EventHubBuiltContextCapture>? observe = null) =>
        new ServiceCollection()
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddViciOneServiceBus(bus =>
            {
                bus.Limits(MessageLimits.Conservative);
                bus.UsingInMemory();
                if (selected) bus.AddRider(rider =>
                {
                    void ConfigureHubs(IRiderRegistrationContext _, IEventHubFactoryConfigurator hubs)
                    {
                        hubs.Host("Endpoint=sb://policy.servicebus.windows.net/;SharedAccessKeyName=research;SharedAccessKey=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
                        hubs.Storage("UseDevelopmentStorage=true");
                        hubs.ReceiveEndpoint("policy-eh", "policy-group", endpoint =>
                        {
                            endpoint.ContainerName = "policy-container";
                            endpoint.CheckpointMessageCount = 1;
                            endpoint.CheckpointMessageLimit = 2;
                            endpoint.CheckpointInterval = TimeSpan.FromMinutes(1);
                            endpoint.PrefetchCount = 1;
                            endpoint.ConcurrentMessageLimit = 1;
                            endpoint.ConcurrentDeliveryLimit = 1;
                            configure(endpoint);
                        });
                    }
                    if (observe is null) rider.UsingEventHub(ConfigureHubs);
                    else
                    {
                        var capture = new EventHubBuiltContextCapture(MessageLimits.Conservative, ConfigureHubs);
                        observe(capture);
                        rider.SetRiderFactory<IEventHubRider>(capture);
                    }
                });
            }).BuildServiceProvider(true);

    private static async Task ResolveAndValidateAsync(ServiceProvider provider)
    {
        Assert.NotNull(provider.GetRequiredService<IBusControl>());
        IHostedService validator = Assert.Single(provider.GetServices<IHostedService>(),
            value => value.GetType().Name.StartsWith("BusCompositionStartupValidator", StringComparison.Ordinal));
        await validator.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
    }
    private static void ApplyInvalid(IEventHubReceiveEndpointConfigurator endpoint, string mode)
    {
        switch (mode)
        {
            case "count-zero": endpoint.CheckpointMessageCount = 0; break;
            case "limit-zero": endpoint.CheckpointMessageLimit = 0; break;
            case "interval-zero": endpoint.CheckpointInterval = TimeSpan.Zero; break;
            case "interval-negative": endpoint.CheckpointInterval = TimeSpan.FromMilliseconds(-2); break;
            case "interval-infinite": endpoint.CheckpointInterval = System.Threading.Timeout.InfiniteTimeSpan; break;
            case "interval-overflow": endpoint.CheckpointInterval = TimeSpan.FromMilliseconds(MaximumTimerMilliseconds + 1); break;
            case "delivery-zero": endpoint.ConcurrentDeliveryLimit = 0; break;
            case "delivery-negative": endpoint.ConcurrentDeliveryLimit = -1; break;
            case "prefetch-zero": endpoint.PrefetchCount = 0; break;
            case "prefetch-negative": endpoint.PrefetchCount = -1; break;
            case "concurrent-zero": endpoint.ConcurrentMessageLimit = 0; break;
            case "concurrent-negative": endpoint.ConcurrentMessageLimit = -1; break;
            default: throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }
    private static string Diagnostic(Exception error)
    {
        var parts = new List<string>();
        for (Exception? current = error; current is not null; current = current.InnerException)
        {
            parts.Add(current.Message);
            if (current is ConfigurationException configuration) parts.AddRange(configuration.Results.Select(result => result.Key + ": " + result.Message));
        }
        return string.Join("\n", parts);
    }
    private sealed record Policy(string Entity, string Group, string Container, ushort Limit, ushort Count,
        int Prefetch, TimeSpan Interval, int Concurrent, int Delivery);
    private static Policy Capture(ReceiveSettings settings) => new(settings.EventHubName, settings.ConsumerGroup, settings.ContainerName,
        settings.CheckpointMessageLimit, settings.CheckpointMessageCount, settings.PrefetchCount, settings.CheckpointInterval,
        settings.ConcurrentMessageLimit, settings.ConcurrentDeliveryLimit);
    private sealed class EmptyBuilder : ProcessorClientBuilderContext
    {
        public Task OnPartitionInitializingAsync(PartitionInitializingEventArgs args, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task OnPartitionClosingAsync(PartitionClosingEventArgs args, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
