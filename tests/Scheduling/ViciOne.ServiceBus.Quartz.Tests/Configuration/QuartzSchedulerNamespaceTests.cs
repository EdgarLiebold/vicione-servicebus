using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Configuration;

public sealed class QuartzSchedulerNamespaceTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-OWNERSHIP", "namespace-identity-excludes-assembly-version")]
    public void BusIdentity_IsAssemblyQualifiedWithoutVersionOrRuntimeMetadata()
    {
        Type busType = typeof(StableBusMarker);
        string expected = $"{busType.Assembly.GetName().Name}:{busType.FullName}";

        string identity = QuartzSchedulerNamespace.GetStableBusIdentity(busType);

        Assert.Equal(expected, identity);
        Assert.DoesNotContain("Version=", identity, StringComparison.Ordinal);
        Assert.DoesNotContain("Culture=", identity, StringComparison.Ordinal);
        Assert.DoesNotContain("PublicKeyToken=", identity, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-OWNERSHIP", "generic-bus-identity-is-recursively-stable")]
    public void GenericBusIdentity_QualifiesEveryTypeWithoutAssemblyVersion()
    {
        Type markerType = typeof(GenericBusMarker<StableBusMarker>);
        string assemblyName = markerType.Assembly.GetName().Name!;
        string genericDefinition = markerType.GetGenericTypeDefinition().FullName!.Split('`')[0];
        string expected = $"{assemblyName}:{genericDefinition}[{assemblyName}:{typeof(StableBusMarker).FullName}]";

        string identity = QuartzSchedulerNamespace.GetStableBusIdentity(markerType);

        Assert.Equal(expected, identity);
        Assert.Equal(
            QuartzSchedulerNamespace.ForBus(markerType),
            QuartzSchedulerNamespace.ForBus(markerType));
    }

    private sealed class StableBusMarker;

    private sealed class GenericBusMarker<TBus>
        where TBus : class;
}
