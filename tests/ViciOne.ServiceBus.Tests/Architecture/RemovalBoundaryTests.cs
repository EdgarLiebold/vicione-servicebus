using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Architecture;

public sealed class RemovalBoundaryTests
{
    private static readonly string[] RemovedNamespaces =
    [
        "ViciOne.ServiceBus.Licensing",
        "ViciOne.ServiceBus.UsageTelemetry",
        "ViciOne.ServiceBus.UsageTracking",
    ];

    private static readonly string[] RemovedTypeNames =
    [
        "BusUsageTelemetry",
        "EndpointUsageTelemetry",
        "HostUsageTelemetry",
        "IUsageTelemetrySource",
        "IUsageTracker",
        "InvalidLicenseException",
        "InvalidLicenseFormatException",
        "LicenseContact",
        "LicenseCustomer",
        "LicenseFeature",
        "LicenseFile",
        "LicenseInfo",
        "LicenseProduct",
        "LicenseReader",
        "LicenseSettings",
        "RiderUsageTelemetry",
        "UsageTelemetryBusObserver",
        "UsageTelemetryConfigurationObserver",
        "UsageTelemetryEndpointConfigurationObserver",
        "UsageTelemetryOptions",
        "UsageTelemetryOptionsExtensions",
        "UsageTelemetrySerializerContext",
        "UsageTracker",
        "ViciOneServiceBusUsageTelemetry",
        "ViciOneServiceBusUsageTelemetryExtensions",
    ];

    private static readonly string[] RemovedConfigurationKeys =
    [
        "UseLicenseFile",
        "VICIONE_SERVICEBUS_LICENSE",
        "VICIONE_SERVICEBUS_LICENSE_PATH",
    ];

    private const string RetainedNeighbour = "ResourceCache`1";

    private static readonly string[] RetiredPropertyMetadataTypes =
    [
        "ViciOne.ServiceBus.Internals.IReadOnlyPropertyCache`1",
        "ViciOne.ServiceBus.Internals.IReadWritePropertyCache`1",
        "ViciOne.ServiceBus.Internals.ReadOnlyProperty",
        "ViciOne.ServiceBus.Internals.ReadOnlyProperty`1",
        "ViciOne.ServiceBus.Internals.ReadOnlyProperty`2",
        "ViciOne.ServiceBus.Internals.ReadOnlyPropertyCache`1",
        "ViciOne.ServiceBus.Internals.ReadWriteProperty",
        "ViciOne.ServiceBus.Internals.ReadWriteProperty`1",
        "ViciOne.ServiceBus.Internals.ReadWriteProperty`2",
        "ViciOne.ServiceBus.Internals.ReadWritePropertyCache`1",
    ];

    private static readonly string[] OwnedPropertyMetadataTypes =
    [
        "ViciOne.ServiceBus.Metadata.IReadOnlyPropertyCache`1",
        "ViciOne.ServiceBus.Metadata.IReadWritePropertyCache`1",
        "ViciOne.ServiceBus.Metadata.PropertyAccessPolicy",
        "ViciOne.ServiceBus.Metadata.ReadOnlyProperty",
        "ViciOne.ServiceBus.Metadata.ReadOnlyProperty`1",
        "ViciOne.ServiceBus.Metadata.ReadOnlyProperty`2",
        "ViciOne.ServiceBus.Metadata.ReadOnlyPropertyCache`1",
        "ViciOne.ServiceBus.Metadata.ReadWriteProperty",
        "ViciOne.ServiceBus.Metadata.ReadWriteProperty`1",
        "ViciOne.ServiceBus.Metadata.ReadWriteProperty`2",
        "ViciOne.ServiceBus.Metadata.ReadWritePropertyCache`1",
    ];

    [Fact]
    [RequirementCoverage("REQ-VSB-REMOVED-CLOSURES", "metadata-namespaces-types-and-retained-control")]
    public void ProductMetadata_ContainsNoRemovedTypeAndStillContainsTheRetainedResourceCache()
    {
        Type[] types = ProductAssemblies().SelectMany(assembly => assembly.GetTypes()).ToArray();

        string[] namespaceOffenders = types
            .Where(type => RemovedNamespaces.Contains(type.Namespace, StringComparer.Ordinal))
            .Select(type => Assert.IsType<string>(type.FullName))
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] nameOffenders = types
            .Where(type => RemovedTypeNames.Contains(type.Name, StringComparer.Ordinal))
            .Select(type => Assert.IsType<string>(type.FullName))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(namespaceOffenders);
        Assert.Empty(nameOffenders);
        Assert.Contains(types, type => type.Name == RetainedNeighbour);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REMOVED-CLOSURES", "default-container-has-no-removed-registration")]
    public void DefaultRegistration_ContainsNoRemovedServiceOrImplementationType()
    {
        IServiceCollection services = new ServiceCollection()
            .AddViciOneServiceBus(configuration => configuration.UsingInMemory());

        Type[] registeredTypes = services
            .SelectMany(descriptor => new[] { descriptor.ServiceType, descriptor.ImplementationType })
            .Where(type => type is not null)
            .Cast<Type>()
            .ToArray();
        string[] offenders = registeredTypes
            .Where(type => RemovedNamespaces.Contains(type.Namespace, StringComparer.Ordinal)
                || RemovedTypeNames.Contains(type.Name, StringComparer.Ordinal))
            .Select(type => Assert.IsType<string>(type.FullName))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(services);
        Assert.Empty(offenders);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REMOVED-CLOSURES", "assembly-bytes-and-retained-control")]
    public void ProductAssemblyBytes_ContainNoRemovedIdentityAndDoContainTheRetainedNeighbour()
    {
        string[] strings = ProductAssemblies().SelectMany(AssemblyStrings).Distinct(StringComparer.Ordinal).ToArray();
        string[] removed = RemovedTypeNames.Concat(RemovedConfigurationKeys).Concat(RemovedNamespaces).ToArray();

        string[] offenders = strings.Where(text => removed.Contains(text, StringComparer.Ordinal)).ToArray();

        Assert.Empty(offenders);
        Assert.Contains(RetainedNeighbour, strings);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REMOVED-CLOSURES", "two-distinct-product-assemblies")]
    public void RemovalScans_ReadBothDistinctProductAssemblies()
    {
        Assembly[] assemblies = ProductAssemblies().ToArray();
        string[] names = assemblies.Select(assembly => Assert.IsType<string>(assembly.GetName().Name)).ToArray();

        Assert.Equal(2, assemblies.Length);
        Assert.Equal(2, names.Distinct(StringComparer.Ordinal).Count());
        Assert.All(assemblies, assembly => Assert.NotEmpty(assembly.GetTypes()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-OWNER", "single-metadata-namespace")]
    public void PropertyMetadataTypes_HaveOneExplicitNamespaceOwner()
    {
        Type[] types = ProductAssemblies().SelectMany(assembly => assembly.GetTypes()).ToArray();
        string[] names = types.Select(type => Assert.IsType<string>(type.FullName)).ToArray();

        string[] retired = names.Intersect(RetiredPropertyMetadataTypes, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        string[] owned = names.Intersect(OwnedPropertyMetadataTypes, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

        Assert.Empty(retired);
        Assert.Equal(OwnedPropertyMetadataTypes.Order(StringComparer.Ordinal), owned);
    }

    internal static IEnumerable<Assembly> ProductAssemblies()
    {
        yield return typeof(IBus).Assembly;
        yield return typeof(InMemoryConfigurationExtensions).Assembly;
    }

    internal static IEnumerable<string> AssemblyStrings(Assembly assembly)
    {
        byte[] bytes = File.ReadAllBytes(assembly.Location);
        var builder = new System.Text.StringBuilder();

        foreach (byte value in bytes)
        {
            if (value is >= 0x20 and < 0x7f)
            {
                builder.Append((char)value);
                continue;
            }

            if (builder.Length > 0)
                yield return builder.ToString();
            builder.Clear();
        }

        if (builder.Length > 0)
            yield return builder.ToString();
    }
}
