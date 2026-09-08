using System.Reflection;
using Quartz;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Configuration;

public sealed class QuartzConfigurationContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-PUBLIC-API", "minimal-greenfield-surface")]
    public void PublicApi_ContainsOnlyOptionsAndQuartzSchedulingComposition()
    {
        Assembly assembly = typeof(QuartzEndpointOptions).Assembly;
        string[] exportedTypes = assembly.GetExportedTypes()
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "ViciOne.ServiceBus.Quartz.QuartzEndpointOptions",
                "ViciOne.ServiceBus.Quartz.QuartzSchedulerLease",
                "ViciOne.ServiceBus.Quartz.QuartzSchedulerOptions",
                "ViciOne.ServiceBus.Quartz.QuartzSchedulingExtensions",
            ],
            exportedTypes);

        Type extensions = Assert.IsAssignableFrom<Type>(
            assembly.GetType("ViciOne.ServiceBus.Quartz.QuartzSchedulingExtensions"));
        string[] methodNames = extensions
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "AddQuartzScheduling",
                "AddQuartzScheduling",
                "ConfigureInMemoryQuartzScheduler",
                "ConfigureQuartzScheduler",
                "ConfigureQuartzScheduling",
                "UseInMemoryQuartzScheduler",
                "UseQuartzScheduler",
            ],
            methodNames);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-SETTINGS", "no-eager-or-ambiguous-factory-option")]
    public void SchedulerOptions_ContainOnlyRuntimeBehaviorSettings()
    {
        string[] properties = typeof(QuartzSchedulerOptions)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                nameof(QuartzSchedulerOptions.ConcurrentMessageLimit),
                nameof(QuartzSchedulerOptions.DeliveryRetryPolicy),
                nameof(QuartzSchedulerOptions.PrefetchCount),
                nameof(QuartzSchedulerOptions.QueueName),
                nameof(QuartzSchedulerOptions.StartDelay),
                nameof(QuartzSchedulerOptions.StartScheduler),
                nameof(QuartzSchedulerOptions.TimeProvider),
                nameof(QuartzSchedulerOptions.TimeZoneResolver),
                nameof(QuartzSchedulerOptions.WaitForJobsToComplete),
            ],
            properties);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-PUBLIC-API", "required-owners-reject-null")]
    public void PublicEntryPoints_RejectMissingRequiredOwners()
    {
        AssertParameter("configurator", () => QuartzSchedulingExtensions.ConfigureInMemoryQuartzScheduler(null!));
        AssertParameter("configurator", () => QuartzSchedulingExtensions.ConfigureQuartzScheduler(null!, null!));
        AssertParameter("configurator", () => QuartzSchedulingExtensions.AddQuartzScheduling(null!, static _ => null!));
        AssertParameter("configurator", () => QuartzSchedulingExtensions.ConfigureQuartzScheduling(null!, null!));
        AssertParameter("configurator", () => QuartzSchedulingExtensions.UseInMemoryQuartzScheduler<IBus>(null!));
        AssertParameter("configurator", () => QuartzSchedulingExtensions.UseQuartzScheduler<IBus>(null!, static _ => null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-PUBLIC-API", "required-collaborators-reject-null")]
    public void PublicEntryPoints_RejectMissingRequiredCollaborators()
    {
        IBusFactoryConfigurator bus = DispatchProxy.Create<IBusFactoryConfigurator, NoOpDispatchProxy>();
        IReceiveEndpointConfigurator endpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, NoOpDispatchProxy>();

        AssertParameter("schedulerFactory", () => QuartzSchedulingExtensions.ConfigureQuartzScheduler(bus, null!));
        AssertParameter("context", () => QuartzSchedulingExtensions.ConfigureQuartzScheduling(endpoint, null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-PUBLIC-API", "missing-endpoint-address-fails-closed")]
    public void ConfigureQuartzScheduler_RejectsATransportThatDoesNotConfigureTheEndpoint()
    {
        IBusFactoryConfigurator configurator =
            DispatchProxy.Create<IBusFactoryConfigurator, DefaultValueDispatchProxy>();

        ConfigurationException failure = Assert.Throws<ConfigurationException>(() =>
            configurator.ConfigureQuartzScheduler(DispatchProxy.Create<ISchedulerFactory, NoOpDispatchProxy>()));

        Assert.Contains("did not expose an input address", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-OWNERSHIP", "owned-factory-is-released-after-configuration-failure")]
    public void ConfigureInMemoryQuartzScheduler_PreservesConfigurationFailureAfterReleasingItsFactory()
    {
        IBusFactoryConfigurator configurator =
            DispatchProxy.Create<IBusFactoryConfigurator, DefaultValueDispatchProxy>();
        var expected = new InvalidOperationException("invalid-scheduler-configuration");

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            configurator.ConfigureInMemoryQuartzScheduler(_ => throw expected));

        Assert.Same(expected, failure);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-SCHEDULER-API", "provider-owned-configuration-scope")]
    public void ReliableSchedulerAdapters_RequireProviderOwnedConfiguration()
    {
        IReliableMessagingConfigurator<IBus> configurator =
            DispatchProxy.Create<IReliableMessagingConfigurator<IBus>, NoOpDispatchProxy>();

        ConfigurationException quartzFailure = Assert.Throws<ConfigurationException>(() =>
            configurator.UseQuartzScheduler(static _ => null!));
        ConfigurationException inMemoryFailure = Assert.Throws<ConfigurationException>(() =>
            configurator.UseInMemoryQuartzScheduler());

        Assert.Contains("UseReliableMessaging", quartzFailure.Message, StringComparison.Ordinal);
        Assert.Contains("UseReliableMessaging", inMemoryFailure.Message, StringComparison.Ordinal);
    }

    private static void AssertParameter(string expected, Action action)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(expected, exception.ParamName);
    }

    private class NoOpDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    private class DefaultValueDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => null;
    }
}
