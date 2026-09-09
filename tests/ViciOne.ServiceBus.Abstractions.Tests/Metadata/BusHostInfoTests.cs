using System.Reflection;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Metadata;

public sealed class BusHostInfoTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-METADATA", "current-process-snapshot")]
    public void CachedHost_DescribesTheCurrentProcessAndRuntime()
    {
        HostInfo host = HostMetadataCache.Host;
        Assembly entryAssembly = Assembly.GetEntryAssembly()!;

        Assert.Equal(Environment.MachineName, host.MachineName);
        Assert.Equal(Environment.ProcessId, host.ProcessId);
        Assert.False(string.IsNullOrWhiteSpace(host.ProcessName));
        Assert.Equal(entryAssembly.GetName().Name, host.Assembly);
        Assert.Equal(entryAssembly.GetName().Version?.ToString() ?? "Unknown", host.AssemblyVersion);
        Assert.Equal(Environment.Version.ToString(), host.FrameworkVersion);
        Assert.Equal(typeof(HostInfo).Assembly.GetName().Version?.ToString(), host.ViciOneServiceBusVersion);
        Assert.Equal(Environment.OSVersion.ToString(), host.OperatingSystemVersion);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-METADATA", "stable-cache")]
    public void HostAndEmptySnapshots_AreStableAndDistinct()
    {
        Assert.Same(HostMetadataCache.Host, HostMetadataCache.Host);
        Assert.Same(HostMetadataCache.Empty, HostMetadataCache.Empty);
        Assert.NotSame(HostMetadataCache.Host, HostMetadataCache.Empty);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-METADATA", "empty-wire-defaults")]
    public void EmptyHost_HasOnlyWireDefaults()
    {
        HostInfo empty = HostMetadataCache.Empty;

        Assert.Null(empty.MachineName);
        Assert.Null(empty.ProcessName);
        Assert.Equal(0, empty.ProcessId);
        Assert.Null(empty.Assembly);
        Assert.Null(empty.AssemblyVersion);
        Assert.Null(empty.FrameworkVersion);
        Assert.Null(empty.ViciOneServiceBusVersion);
        Assert.Null(empty.OperatingSystemVersion);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-METADATA", "unambiguous-public-construction")]
    public void PublicConstruction_ContainsOnlyTheParameterlessWireConstructor()
    {
        ConstructorInfo constructor = Assert.Single(typeof(BusHostInfo).GetConstructors());

        Assert.Empty(constructor.GetParameters());
        Assert.True(typeof(BusHostInfo).IsSealed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-METADATA", "runtime-neutral-environment-api")]
    public void EnvironmentApi_UsesCurrentPlatformTermsWithoutFrameworkCompatibilityState()
    {
        string[] environmentProperties = typeof(HostMetadataCache)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(bool))
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["IsRunningInContainer", "IsRunningInKubernetes"], environmentProperties);
    }
}
