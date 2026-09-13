using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class TestingServiceCollectionExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TELEMETRY", "registration-overloads-and-test-identity")]
    public void TestTelemetryRegistration_BindsTheRequestedWriterDetailsAndCallingTestIdentity()
    {
        var defaultServices = new ServiceCollection();
        Assert.Same(defaultServices, defaultServices.AddViciOneServiceBusTestTelemetry(includeDetails: false));
        Assert.Single(defaultServices, descriptor => descriptor.ServiceType == typeof(TestActivityListener));

        var writer = new StringWriter();
        var services = new ServiceCollection();
        Assert.Same(services, services.AddViciOneServiceBusTestTelemetry(writer, includeDetails: true));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(TestActivityListener));

        ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        TestActivityListener listener = provider.GetRequiredService<TestActivityListener>();
        using (var source = new ActivitySource("ViciOne.ServiceBus.Tests.Testing.TelemetryRegistration"))
        using (Activity? activity = source.StartActivity("registered-operation"))
        {
            Assert.NotNull(activity);
            activity.AddTag("test.detail", "expected");
        }

        Assert.True(listener.DisposeAsync().IsCompletedSuccessfully);
        Assert.True(provider.DisposeAsync().IsCompletedSuccessfully);
        string output = writer.ToString();
        Assert.Contains(nameof(TestingServiceCollectionExtensionsTests), output, StringComparison.Ordinal);
        Assert.Contains(nameof(TestTelemetryRegistration_BindsTheRequestedWriterDetailsAndCallingTestIdentity), output,
            StringComparison.Ordinal);
        Assert.Contains("registered-operation", output, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-RETENTION", "registration-applies-mode-and-capacity")]
    public void ContextRetentionRegistration_AppliesTheExactModeAndCapacity()
    {
        var services = new ServiceCollection();
        IBusRegistrationConfigurator? callbackConfigurator = null;

        services.AddViciOneServiceBusTestHarness(configurator =>
        {
            callbackConfigurator = configurator;
            Assert.Same(
                configurator,
                configurator.SetTestContextRetention(TestContextSaveMode.Bounded, maximumSavedContexts: 17));
        });

        ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        TestHarnessOptions options = provider.GetRequiredService<IOptions<TestHarnessOptions>>().Value;

        Assert.NotNull(callbackConfigurator);
        Assert.Equal(TestContextSaveMode.Bounded, options.ContextSaveMode);
        Assert.Equal(17, options.MaximumSavedContexts);
        Assert.True(provider.DisposeAsync().IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-REGISTRATION", "telemetry-and-retention-boundaries")]
    public void TelemetryAndRetentionRegistration_RejectEveryInvalidRequiredArgument()
    {
        Assert.Equal("services", Assert.Throws<ArgumentNullException>(() =>
            ViciOneServiceBusTestingServiceCollectionExtensions.AddViciOneServiceBusTestTelemetry(null!)).ParamName);
        Assert.Equal("textWriter", Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddViciOneServiceBusTestTelemetry(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            ViciOneServiceBusTestingServiceCollectionExtensions.SetTestContextRetention(
                null!,
                TestContextSaveMode.All)).ParamName);

        Assert.Equal("saveMode", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ServiceCollection().AddViciOneServiceBusTestHarness(configurator =>
                configurator.SetTestContextRetention((TestContextSaveMode)int.MaxValue))).ParamName);
        Assert.Equal("maximumSavedContexts", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ServiceCollection().AddViciOneServiceBusTestHarness(configurator =>
                configurator.SetTestContextRetention(TestContextSaveMode.Bounded, 0))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-REGISTRATION", "reachable-registration-surface-forwarding")]
    public async Task HarnessRegistration_ForwardsTheReachableCoreAndAdvancedConfigurationSurfaceAsync()
    {
        var expectedTimeout = new RequestTimeout(TimeSpan.FromSeconds(17));
        RequestTimeout? observedClientFactoryTimeout = null;
        var riderConfigured = false;
        TimeSpan operationTimeout = TimeSpan.FromSeconds(10);
        var endpointNameFormatter = new KebabCaseEndpointNameFormatter("testing", includeNamespace: false);
        var destinationAddress = new Uri("loopback://localhost/explicit-registration-request");
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configurator =>
            {
                Assert.Equal("endpointDefinition", Assert.Throws<ArgumentNullException>(() =>
                    configurator.AddEndpoint(null!)).ParamName);

                configurator.SetTestTimeouts(operationTimeout, operationTimeout);
                configurator.AddEndpoint(typeof(StandaloneEndpointDefinition));
                configurator.SetDefaultRequestTimeout(expectedTimeout);
                configurator.AddRequestClient(typeof(RegistrationRequest));
                configurator.AddRequestClient<GenericRegistrationRequest>(expectedTimeout);
                configurator.AddRequestClient<GenericDestinationRegistrationRequest>(destinationAddress, expectedTimeout);
                configurator.AddRequestClient(typeof(RuntimeDestinationRegistrationRequest), destinationAddress, expectedTimeout);
                configurator.SetEndpointNameFormatter(endpointNameFormatter);
                configurator.AddRider(_ => riderConfigured = true);
                configurator.SetRequestClientFactory((bus, timeout) =>
                {
                    observedClientFactoryTimeout = timeout;
                    return bus.CreateClientFactory(timeout);
                });
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(
            cancellationToken: TestContext.Current.CancellationToken).WaitAsync(
                operationTimeout,
                TestContext.Current.CancellationToken);

        try
        {
            StandaloneEndpointDefinition definition = provider.GetRequiredService<StandaloneEndpointDefinition>();
            IEndpointRegistration registration = Assert.Single(
                provider.GetServices<IEndpointRegistration>(),
                candidate => candidate.Type == typeof(StandaloneEndpointMarker));
            IClientFactory clientFactory = provider.GetRequiredService<IClientFactory>();
            using IServiceScope scope = provider.CreateScope();
            IRequestClient<RegistrationRequest> first = scope.ServiceProvider.GetRequiredService<IRequestClient<RegistrationRequest>>();
            IRequestClient<RegistrationRequest> second = scope.ServiceProvider.GetRequiredService<IRequestClient<RegistrationRequest>>();
            IRequestClient<GenericRegistrationRequest> generic =
                scope.ServiceProvider.GetRequiredService<IRequestClient<GenericRegistrationRequest>>();
            IRequestClient<GenericDestinationRegistrationRequest> genericDestination =
                scope.ServiceProvider.GetRequiredService<IRequestClient<GenericDestinationRegistrationRequest>>();
            IRequestClient<RuntimeDestinationRegistrationRequest> runtimeDestination =
                scope.ServiceProvider.GetRequiredService<IRequestClient<RuntimeDestinationRegistrationRequest>>();

            Assert.True(riderConfigured);
            Assert.Equal(expectedTimeout, observedClientFactoryTimeout);
            Assert.Same(endpointNameFormatter, provider.GetRequiredService<IEndpointNameFormatter>());
            Assert.True(registration.IncludeInConfigureEndpoints);
            Assert.Same(definition, registration.GetDefinition(provider));
            Assert.Equal(1, definition.ConfigureCount);
            Assert.Same(first, second);
            Assert.Same(generic, scope.ServiceProvider.GetRequiredService<IRequestClient<GenericRegistrationRequest>>());
            Assert.Same(
                genericDestination,
                scope.ServiceProvider.GetRequiredService<IRequestClient<GenericDestinationRegistrationRequest>>());
            Assert.Same(
                runtimeDestination,
                scope.ServiceProvider.GetRequiredService<IRequestClient<RuntimeDestinationRegistrationRequest>>());
            Assert.Same(clientFactory, provider.GetRequiredService<IClientFactory>());
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    private sealed record RegistrationRequest;

    private sealed record GenericRegistrationRequest;

    private sealed record GenericDestinationRegistrationRequest;

    private sealed record RuntimeDestinationRegistrationRequest;

    private sealed class StandaloneEndpointMarker;

    private sealed class StandaloneEndpointDefinition : IEndpointDefinition<StandaloneEndpointMarker>
    {
        private int _configureCount;

        public bool IsTemporary => true;

        public int? PrefetchCount => 1;

        public int? ConcurrentMessageLimit => 1;

        public bool ConfigureConsumeTopology => false;

        public int ConfigureCount => Volatile.Read(ref _configureCount);

        public string GetEndpointName(IEndpointNameFormatter formatter) => "standalone-registration-probe";

        public void Configure<TEndpointConfigurator>(TEndpointConfigurator configurator, IRegistrationContext? context = null)
            where TEndpointConfigurator : IReceiveEndpointConfigurator =>
            Interlocked.Increment(ref _configureCount);
    }
}
