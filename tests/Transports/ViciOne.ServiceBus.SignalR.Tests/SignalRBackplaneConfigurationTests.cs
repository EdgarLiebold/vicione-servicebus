using System.Reflection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.SignalR.Configuration;
using ViciOne.ServiceBus.SignalR.Consumers;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.SignalR.Runtime;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SignalR.Tests;

public sealed class SignalRBackplaneConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-COMPOSITION", "default-remote-group-timeout")]
    public void Options_DefaultToTheDocumentedRemoteGroupTimeout()
    {
        var options = new SignalRBackplaneOptions();

        Assert.Equal(TimeSpan.FromSeconds(20), options.RemoteGroupOperationTimeout);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-COMPOSITION", "custom-remote-group-timeout")]
    public void Registration_AppliesACustomRemoteGroupTimeout()
    {
        var services = new ServiceCollection();
        var configurator = new ServiceCollectionBusConfigurator(services);
        TimeSpan expected = TimeSpan.FromSeconds(7);

        configurator.AddSignalRBackplane<ConfigurationHub>(options =>
            options.RemoteGroupOperationTimeout = expected);

        ServiceDescriptor descriptor = Assert.Single(services, candidate =>
            candidate.ServiceType == typeof(SignalRBackplaneSettings<ConfigurationHub>));
        var settings = Assert.IsType<SignalRBackplaneSettings<ConfigurationHub>>(
            descriptor.ImplementationInstance);
        Assert.Equal(expected, settings.RemoteGroupOperationTimeout.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-COMPOSITION", "registered-options-are-snapshotted")]
    public void Registration_SnapshotsOptionsBeforeCallerMutatesThem()
    {
        var services = new ServiceCollection();
        var configurator = new ServiceCollectionBusConfigurator(services);
        SignalRBackplaneOptions? capturedOptions = null;
        TimeSpan registeredTimeout = TimeSpan.FromSeconds(7);

        configurator.AddSignalRBackplane<ConfigurationHub>(options =>
        {
            options.RemoteGroupOperationTimeout = registeredTimeout;
            capturedOptions = options;
        });
        Assert.IsType<SignalRBackplaneOptions>(capturedOptions).RemoteGroupOperationTimeout =
            TimeSpan.FromSeconds(13);

        ServiceDescriptor descriptor = Assert.Single(services, candidate =>
            candidate.ServiceType == typeof(SignalRBackplaneSettings<ConfigurationHub>));
        var settings = Assert.IsType<SignalRBackplaneSettings<ConfigurationHub>>(
            descriptor.ImplementationInstance);
        Assert.Equal(registeredTimeout, settings.RemoteGroupOperationTimeout.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-2)]
    [RequirementCoverage("REQ-VSB-SIGNALR-COMPOSITION", "nonpositive-timeout-rejected-atomically")]
    public void Registration_RejectsANonPositiveRemoteGroupOperationTimeout(int milliseconds)
    {
        var services = new ServiceCollection();
        var configurator = new ServiceCollectionBusConfigurator(services);
        int descriptorCount = services.Count;

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            configurator.AddSignalRBackplane<ConfigurationHub>(options =>
                options.RemoteGroupOperationTimeout = TimeSpan.FromMilliseconds(milliseconds)));

        Assert.Equal(
            "SignalR backplane for bus 'registration': RemoteGroupOperationTimeout must be greater than zero. "
                + "Set RemoteGroupOperationTimeout to a positive duration.",
            exception.Message);
        Assert.Equal(descriptorCount, services.Count);
        Assert.DoesNotContain(services, descriptor =>
            descriptor.ServiceType == typeof(HubLifetimeManager<ConfigurationHub>));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SIGNALR-COMPOSITION", "unusable-remote-group-timeout-rejected-before-registration")]
    public void Registration_RejectsTimeoutBeyondTheSystemTimerLimit(bool useMaximum)
    {
        var services = new ServiceCollection();
        var configurator = new ServiceCollectionBusConfigurator(services);
        int descriptorCount = services.Count;
        TimeSpan timeout = useMaximum
            ? TimeSpan.MaxValue
            : TimeSpan.FromMilliseconds(4294967295L);

        ConfigurationException failure = Assert.Throws<ConfigurationException>(() =>
            configurator.AddSignalRBackplane<ConfigurationHub>(options =>
                options.RemoteGroupOperationTimeout = timeout));

        Assert.Contains(nameof(SignalRBackplaneOptions.RemoteGroupOperationTimeout), failure.Message, StringComparison.Ordinal);
        Assert.Equal(descriptorCount, services.Count);
        Assert.DoesNotContain(services, descriptor =>
            descriptor.ServiceType == typeof(HubLifetimeManager<ConfigurationHub>));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SIGNALR-COMPOSITION", "maximum-system-timer-timeout-is-registrable")]
    public void Registration_AcceptsTheLargestSystemTimerTimeout(bool fractionalMaximum)
    {
        var services = new ServiceCollection();
        var configurator = new ServiceCollectionBusConfigurator(services);
        TimeSpan maximum = fractionalMaximum
            ? TimeSpan.FromMilliseconds(4294967295L) - TimeSpan.FromTicks(1)
            : TimeSpan.FromMilliseconds(4294967294L);

        configurator.AddSignalRBackplane<ConfigurationHub>(options =>
            options.RemoteGroupOperationTimeout = maximum);

        ServiceDescriptor descriptor = Assert.Single(services, candidate =>
            candidate.ServiceType == typeof(SignalRBackplaneSettings<ConfigurationHub>));
        var settings = Assert.IsType<SignalRBackplaneSettings<ConfigurationHub>>(
            descriptor.ImplementationInstance);
        Assert.Equal(maximum, settings.RemoteGroupOperationTimeout.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-COMPOSITION", "fluent-hub-registration")]
    public void Registration_ReturnsTheConfiguratorAndAddsTheHubLifetimeManager()
    {
        var services = new ServiceCollection();
        var configurator = new ServiceCollectionBusConfigurator(services);

        IBusRegistrationConfigurator returned = configurator.AddSignalRBackplane<ConfigurationHub>();

        Assert.Same(configurator, returned);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(HubLifetimeManager<ConfigurationHub>));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-COMPOSITION", "shared-hub-lifetime-manager-instance")]
    public async Task Registration_ResolvesOneSharedLifetimeManagerInstanceAsync()
    {
        var services = new ServiceCollection();
        var configurator = new ServiceCollectionBusConfigurator(services);
        configurator.AddSignalRBackplane<ConfigurationHub>();
        services.AddSingleton<IHubProtocolResolver>(
            new TestHubProtocolResolver([new JsonHubProtocol()]));
        await using ServiceProvider provider = services.BuildServiceProvider();

        HubLifetimeManager<ConfigurationHub> publicManager =
            provider.GetRequiredService<HubLifetimeManager<ConfigurationHub>>();
        ServiceBusHubLifetimeManager<ConfigurationHub> concreteManager =
            provider.GetRequiredService<ServiceBusHubLifetimeManager<ConfigurationHub>>();

        Assert.Same(concreteManager, publicManager);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-COMPOSITION", "duplicate-hub-registration-rejected-atomically")]
    public void Registration_RejectsTheSameHubWithoutPartiallyAddingServices()
    {
        var services = new ServiceCollection();
        var configurator = new ServiceCollectionBusConfigurator(services);
        configurator.AddSignalRBackplane<ConfigurationHub>();
        int descriptorCount = services.Count;

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            configurator.AddSignalRBackplane<ConfigurationHub>());

        Assert.Equal(
            $"SignalR backplane for bus 'registration': Hub '{typeof(ConfigurationHub)}' already has a backplane registration. "
                + "Register each hub type exactly once.",
            exception.Message);
        Assert.Equal(descriptorCount, services.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-COMPOSITION", "independent-hub-types")]
    public void Registration_AllowsIndependentHubTypes()
    {
        var services = new ServiceCollection();
        var configurator = new ServiceCollectionBusConfigurator(services);

        configurator.AddSignalRBackplane<ConfigurationHub>();
        configurator.AddSignalRBackplane<SecondConfigurationHub>();

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(HubLifetimeManager<ConfigurationHub>));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(HubLifetimeManager<SecondConfigurationHub>));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-COMPOSITION", "null-configurator-rejected")]
    public void Registration_RejectsAMissingConfigurator()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            SignalRBackplaneExtensions.AddSignalRBackplane<ConfigurationHub>(null!));

        Assert.Equal("configurator", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-COMPOSITION", "public-api-is-backplane-composition-only")]
    public void PublicApi_ContainsOnlyBackplaneOptionsAndComposition()
    {
        Assembly assembly = typeof(SignalRBackplaneOptions).Assembly;
        string[] exportedTypes = assembly.GetExportedTypes()
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "ViciOne.ServiceBus.SignalR.SignalRBackplaneExtensions",
                "ViciOne.ServiceBus.SignalR.SignalRBackplaneOptions",
            ],
            exportedTypes);

        PropertyInfo property = Assert.Single(typeof(SignalRBackplaneOptions).GetProperties(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Equal(nameof(SignalRBackplaneOptions.RemoteGroupOperationTimeout), property.Name);
    }

    [Fact]
    public void LifetimeManager_RejectsMissingRequiredCollaborators()
    {
        var settings = new SignalRBackplaneSettings<ConfigurationHub>(
            new RequestTimeout(TimeSpan.FromSeconds(1)));
        var protocols = new TestHubProtocolResolver([new JsonHubProtocol()]);
        var scopeProvider = new RejectingBackplaneScopeProvider();
        NullLogger<ServiceBusHubLifetimeManager<ConfigurationHub>> logger =
            NullLogger<ServiceBusHubLifetimeManager<ConfigurationHub>>.Instance;

        Assert.Equal(
            "settings",
            Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new ServiceBusHubLifetimeManager<ConfigurationHub>(null!, scopeProvider, protocols, logger);
            }).ParamName);
        Assert.Equal(
            "scopeProvider",
            Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new ServiceBusHubLifetimeManager<ConfigurationHub>(settings, null!, protocols, logger);
            }).ParamName);
        Assert.Equal(
            "protocolResolver",
            Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new ServiceBusHubLifetimeManager<ConfigurationHub>(settings, scopeProvider, null!, logger);
            }).ParamName);
    }

    [Fact]
    public void LifetimeManager_RejectsAResolverWithoutProtocols()
    {
        var settings = new SignalRBackplaneSettings<ConfigurationHub>(
            new RequestTimeout(TimeSpan.FromSeconds(1)));
        var scopeProvider = new RejectingBackplaneScopeProvider();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            _ = new ServiceBusHubLifetimeManager<ConfigurationHub>(
                settings,
                scopeProvider,
                new EmptyHubProtocolResolver()));

        Assert.Equal("At least one SignalR hub protocol must be registered.", exception.Message);
    }

    [Fact]
    public void EndpointIdentity_IsBoundedDeterministicAndTypeSpecific()
    {
        var formatter = new IdentityEndpointNameFormatter();

        string first = new SignalRBackplaneEndpointDefinition<FirstHubContainer.CollidingHub>()
            .GetEndpointName(formatter);
        string repeated = new SignalRBackplaneEndpointDefinition<FirstHubContainer.CollidingHub>()
            .GetEndpointName(formatter);
        string second = new SignalRBackplaneEndpointDefinition<SecondHubContainer.CollidingHub>()
            .GetEndpointName(formatter);

        Assert.Matches("^signalr-backplane-[0-9a-f]{20}$", first);
        Assert.Equal(first, repeated);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void EndpointDefinition_RejectsAMissingFormatter()
    {
        var definition = new SignalRBackplaneEndpointDefinition<ConfigurationHub>();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            definition.GetEndpointName(null!));

        Assert.Equal("formatter", exception.ParamName);
    }

    [Fact]
    public void ConsumerDefinitions_ShareOnlyASupportedHubEndpoint()
    {
        var endpoint = new SignalRBackplaneEndpointDefinition<ConfigurationHub>();

        Assert.NotNull(new BackplaneConsumerDefinition<BroadcastConsumer<ConfigurationHub>, ConfigurationHub>(endpoint));
        Assert.NotNull(new BackplaneConsumerDefinition<ConnectionConsumer<ConfigurationHub>, ConfigurationHub>(endpoint));
        Assert.NotNull(new BackplaneConsumerDefinition<GroupConsumer<ConfigurationHub>, ConfigurationHub>(endpoint));
        Assert.NotNull(new BackplaneConsumerDefinition<GroupCommandConsumer<ConfigurationHub>, ConfigurationHub>(endpoint));
        Assert.NotNull(new BackplaneConsumerDefinition<UserConsumer<ConfigurationHub>, ConfigurationHub>(endpoint));
        Assert.NotNull(new BackplaneConsumerDefinition<ClientResultConsumer<ConfigurationHub>, ConfigurationHub>(endpoint));
        Assert.NotNull(new BackplaneConsumerDefinition<InvocationCancellationConsumer<ConfigurationHub>, ConfigurationHub>(endpoint));

        Assert.Throws<InvalidOperationException>(() =>
            new BackplaneConsumerDefinition<UnsupportedConsumer, ConfigurationHub>(endpoint));
        Assert.Throws<ArgumentNullException>(() =>
            new BackplaneConsumerDefinition<BroadcastConsumer<ConfigurationHub>, ConfigurationHub>(null!));
    }

    [Fact]
    public async Task DependencyInjectionScope_ResolvesAndDisposesItsScopedCollaboratorsAsync()
    {
        using var harness = new InMemoryTestHarness($"signalr-scope-{NewId.NextGuid():N}");
        await harness.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            await using IClientFactory clientFactory = harness.Bus.CreateClientFactory();
            var services = new ServiceCollection();
            var disposalProbe = new AsyncDisposalProbe();
            IPublishEndpoint publishEndpoint = harness.Bus;
            IRequestClient<GroupCommand<TestHub>> requestClient =
                clientFactory.CreateRequestClient<GroupCommand<TestHub>>();
            services.AddScoped(_ => disposalProbe);
            services.AddScoped<IPublishEndpoint>(serviceProvider =>
            {
                _ = serviceProvider.GetRequiredService<AsyncDisposalProbe>();
                return publishEndpoint;
            });
            services.AddScoped(_ => requestClient);
            await using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var provider = new DependencyInjectionBackplaneScopeProvider(
                serviceProvider.GetRequiredService<IServiceScopeFactory>());

            IBackplaneScope<TestHub> scope = await provider.CreateScopeAsync<TestHub>();

            Assert.Same(publishEndpoint, scope.PublishEndpoint);
            Assert.Same(requestClient, scope.GroupCommandClient);
            Assert.False(disposalProbe.IsDisposed);

            await scope.DisposeAsync();
            Assert.True(disposalProbe.IsDisposed);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task DependencyInjectionScope_ResolutionFailureDisposesAsyncOnlyServicesAsync()
    {
        var services = new ServiceCollection();
        var disposalProbe = new AsyncDisposalProbe();
        services.AddScoped(_ => disposalProbe);
        services.AddScoped<IPublishEndpoint>(provider =>
        {
            _ = provider.GetRequiredService<AsyncDisposalProbe>();
            return CreateProxy<IPublishEndpoint>();
        });
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        var provider = new DependencyInjectionBackplaneScopeProvider(
            serviceProvider.GetRequiredService<IServiceScopeFactory>());

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await provider.CreateScopeAsync<ConfigurationHub>());

        Assert.True(disposalProbe.IsDisposed);
    }

    [Fact]
    public void DependencyInjectionScopeProvider_RejectsAMissingScopeFactory()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new DependencyInjectionBackplaneScopeProvider(null!));

        Assert.Equal("serviceScopeFactory", exception.ParamName);
    }

    private static TInterface CreateProxy<TInterface>()
        where TInterface : class =>
        DispatchProxy.Create<TInterface, ThrowingDispatchProxy>();

    private sealed class ConfigurationHub : Hub;

    private sealed class SecondConfigurationHub : Hub;

    private sealed class EmptyHubProtocolResolver : IHubProtocolResolver
    {
        public IReadOnlyList<IHubProtocol> AllProtocols => [];

        public IHubProtocol? GetProtocol(string protocolName, IReadOnlyList<string>? supportedProtocols) => null;
    }

    private static class FirstHubContainer
    {
        public sealed class CollidingHub : Hub;
    }

    private static class SecondHubContainer
    {
        public sealed class CollidingHub : Hub;
    }

    private sealed class UnsupportedConsumer : IConsumer;

    private sealed class IdentityEndpointNameFormatter : IEndpointNameFormatter
    {
        public string Separator => "-";

        public string TemporaryEndpoint(string tag) => tag;

        public string Consumer<T>()
            where T : class, IConsumer =>
            throw new NotSupportedException();

        public string Message<T>()
            where T : class =>
            throw new NotSupportedException();

        public string Saga<T>()
            where T : class =>
            throw new NotSupportedException();

        public string ExecuteActivity<T, TArguments>()
            where T : class
            where TArguments : class =>
            throw new NotSupportedException();

        public string CompensateActivity<T, TLog>()
            where T : class
            where TLog : class =>
            throw new NotSupportedException();

        public string SanitizeName(string name) => name;
    }

    private sealed class AsyncDisposalProbe : IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private class ThrowingDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException($"Proxy member '{targetMethod?.Name}' was not expected to run.");
    }

    private sealed class RejectingBackplaneScopeProvider : IBackplaneScopeProvider
    {
        public ValueTask<IBackplaneScope<THub>> CreateScopeAsync<THub>()
            where THub : Hub =>
            throw new NotSupportedException();
    }
}
