using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Monitoring;

public sealed class BusHealthCheckTests
{
    [Theory]
    [InlineData(BusHealthStatus.Healthy, HealthStatus.Unhealthy, HealthStatus.Healthy)]
    [InlineData(BusHealthStatus.Degraded, HealthStatus.Unhealthy, HealthStatus.Degraded)]
    [InlineData(BusHealthStatus.Unhealthy, HealthStatus.Unhealthy, HealthStatus.Unhealthy)]
    [InlineData(BusHealthStatus.Unhealthy, HealthStatus.Degraded, HealthStatus.Degraded)]
    [InlineData(BusHealthStatus.Degraded, HealthStatus.Healthy, HealthStatus.Healthy)]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH", "aggregate-status-and-registration-floor")]
    public async Task CheckHealth_MapsAggregateStatusAndAppliesTheRegistrationFloorAsync(
        BusHealthStatus busStatus,
        HealthStatus failureStatus,
        HealthStatus expected)
    {
        var failure = new ExpectedHealthException();
        BusHealthResult snapshot = CreateSnapshot(busStatus, "snapshot", failure, new Dictionary<string, EndpointHealthResult>());
        (BusHealthCheck check, BusControlProxy control) = CreateHealthCheck(snapshot);

        HealthCheckResult result = await check.CheckHealthAsync(Context(failureStatus), TestContext.Current.CancellationToken);

        Assert.Equal(expected, result.Status);
        Assert.Equal("snapshot", result.Description);
        if (expected == HealthStatus.Healthy)
            Assert.Null(result.Exception);
        else
            Assert.Same(failure, result.Exception);
        Assert.Equal(1, control.CheckHealthCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH", "case-sensitive-endpoint-projection")]
    public async Task EndpointProjection_PreservesCaseDistinctAddressesAndCurrentStatusAsync()
    {
        IReceiveEndpoint upper = CreateEndpoint(new Uri("loopback://localhost/Orders"));
        IReceiveEndpoint lower = CreateEndpoint(new Uri("loopback://localhost/orders"));
        var endpoints = new Dictionary<string, EndpointHealthResult>(StringComparer.Ordinal)
        {
            [upper.InputAddress.ToString()] = EndpointHealthResult.Healthy(upper, "ready"),
            [lower.InputAddress.ToString()] = EndpointHealthResult.Degraded(lower, "recovering"),
        };
        (BusHealthCheck check, _) = CreateHealthCheck(BusHealthResult.Degraded("two endpoints", null, endpoints));

        HealthCheckResult result = await check.CheckHealthAsync(
            Context(HealthStatus.Unhealthy),
            TestContext.Current.CancellationToken);

        object projection = Assert.Single(result.Data).Value;
        string rendered = Assert.IsType<string>(projection.ToString());
        Assert.Contains("loopback://localhost/Orders: Healthy - ready", rendered, StringComparison.Ordinal);
        Assert.Contains("loopback://localhost/orders: Degraded - recovering", rendered, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH", "argument-and-cancellation-boundaries")]
    public async Task Boundaries_RejectMissingInputsAndPreservePreCanceledObservationAsync()
    {
        Assert.Throws<ArgumentNullException>(() => new BusHealthCheck(null!));
        (BusHealthCheck check, BusControlProxy control) = CreateHealthCheck(
            BusHealthResult.Healthy("ready", new Dictionary<string, EndpointHealthResult>()));
        await Assert.ThrowsAsync<ArgumentNullException>(() => check.CheckHealthAsync(null!, CancellationToken.None));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => check.CheckHealthAsync(Context(HealthStatus.Unhealthy), cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(0, control.CheckHealthCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH", "stable-defaults-and-explicit-overrides")]
    public void Registration_UsesStableDefaultsAndReplacesThemWithExplicitOptions()
    {
        using ServiceProvider defaultProvider = CreateProvider(configureHealth: null);
        HealthCheckRegistration defaultRegistration = Assert.Single(
            defaultProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);
        Assert.Equal("vicione-servicebus-bus", defaultRegistration.Name);
        Assert.Equal(HealthStatus.Unhealthy, defaultRegistration.FailureStatus);
        Assert.Equal(
            ["ready", "vicione-servicebus"],
            defaultRegistration.Tags.Order(StringComparer.Ordinal));
        Assert.IsType<BusHealthCheck>(defaultRegistration.Factory(defaultProvider));

        using ServiceProvider configuredProvider = CreateProvider(options =>
        {
            options.Name = "orders-bus";
            options.MinimalFailureStatus = HealthStatus.Degraded;
            options.Tags.Add("orders");
            options.Tags.Add("readiness");
        });
        HealthCheckRegistration configuredRegistration = Assert.Single(
            configuredProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);
        Assert.Equal("orders-bus", configuredRegistration.Name);
        Assert.Equal(HealthStatus.Degraded, configuredRegistration.FailureStatus);
        Assert.Equal(["orders", "readiness"], configuredRegistration.Tags.Order(StringComparer.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH", "configuration-api-boundaries-and-owned-tag-abstraction")]
    public void ConfigurationApi_RejectsMissingInputsAndExposesOnlyTheMutableTagContract()
    {
        IBusRegistrationConfigurator defaultConfigurator =
            DispatchProxy.Create<IBusRegistrationConfigurator, UnexpectedInvocationProxy>();
        IBusRegistrationConfigurator<ISecondaryBus> typedConfigurator =
            DispatchProxy.Create<IBusRegistrationConfigurator<ISecondaryBus>, UnexpectedInvocationProxy>();

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            ViciOneServiceBusHealthCheckOptionsExtensions.ConfigureHealthCheckOptions(
                null!,
                static _ => { })).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
            defaultConfigurator.ConfigureHealthCheckOptions(null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            ViciOneServiceBusHealthCheckOptionsExtensions.ConfigureHealthCheckOptions<ISecondaryBus>(
                null!,
                static _ => { })).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
            typedConfigurator.ConfigureHealthCheckOptions(null!)).ParamName);

        PropertyInfo? tags = typeof(ViciOneServiceBusHealthCheckOptions<>).GetProperty(
            nameof(ViciOneServiceBusHealthCheckOptions<IBus>.Tags));
        Assert.NotNull(tags);
        Assert.Equal(typeof(ISet<string>), tags.PropertyType);
        Assert.DoesNotContain(
            typeof(ViciOneServiceBusHealthCheckOptions<>).Assembly.GetExportedTypes(),
            type => type.Name == "IHealthCheckOptions");
    }

    private static ServiceProvider CreateProvider(Action<IHealthCheckOptionsConfigurator>? configureHealth)
    {
        var services = new ServiceCollection();
        services.AddViciOneServiceBusTextWriterLogger(TextWriter.Null);
        services.AddViciOneServiceBus(configurator =>
        {
            configurator.Limits(MessageLimits.Conservative);
            if (configureHealth is not null)
                configurator.ConfigureHealthCheckOptions(configureHealth);
            configurator.UsingInMemory();
        });
        return services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static (BusHealthCheck Check, BusControlProxy Control) CreateHealthCheck(BusHealthResult snapshot)
    {
        IBusControl busControl = DispatchProxy.Create<IBusControl, BusControlProxy>();
        var control = (BusControlProxy)(object)busControl;
        control.Snapshot = snapshot;
        IBusInstance busInstance = DispatchProxy.Create<IBusInstance, BusInstanceProxy>();
        ((BusInstanceProxy)(object)busInstance).BusControl = busControl;
        return (new BusHealthCheck(busInstance), control);
    }

    private static IReceiveEndpoint CreateEndpoint(Uri inputAddress)
    {
        IReceiveEndpoint endpoint = DispatchProxy.Create<IReceiveEndpoint, ReceiveEndpointProxy>();
        ((ReceiveEndpointProxy)(object)endpoint).InputAddress = inputAddress;
        return endpoint;
    }

    private static BusHealthResult CreateSnapshot(
        BusHealthStatus status,
        string description,
        Exception exception,
        IReadOnlyDictionary<string, EndpointHealthResult> endpoints)
        => status switch
        {
            BusHealthStatus.Healthy => BusHealthResult.Healthy(description, endpoints),
            BusHealthStatus.Degraded => BusHealthResult.Degraded(description, exception, endpoints),
            BusHealthStatus.Unhealthy => BusHealthResult.Unhealthy(description, exception, endpoints),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown bus health status."),
        };

    private static HealthCheckContext Context(HealthStatus failureStatus) => new()
    {
        Registration = new HealthCheckRegistration(
            "test",
            _ => throw new InvalidOperationException("The context registration factory must not be invoked."),
            failureStatus,
            tags: null),
    };

    private class BusControlProxy : DispatchProxy
    {
        public BusHealthResult Snapshot { get; set; } = null!;

        public int CheckHealthCallCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IBusControl.CheckHealth))
            {
                CheckHealthCallCount++;
                return Snapshot;
            }

            throw new InvalidOperationException($"Unexpected bus-control invocation: {targetMethod?.Name}.");
        }
    }

    private class BusInstanceProxy : DispatchProxy
    {
        public IBusControl BusControl { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_BusControl")
                return BusControl;

            throw new InvalidOperationException($"Unexpected bus-instance invocation: {targetMethod?.Name}.");
        }
    }

    private class ReceiveEndpointProxy : DispatchProxy
    {
        public Uri InputAddress { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_InputAddress")
                return InputAddress;

            throw new InvalidOperationException($"Unexpected receive-endpoint invocation: {targetMethod?.Name}.");
        }
    }

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => throw new InvalidOperationException($"Unexpected invocation: {targetMethod?.Name}.");
    }

    private interface ISecondaryBus : IBus;

    private sealed class ExpectedHealthException : Exception;
}
