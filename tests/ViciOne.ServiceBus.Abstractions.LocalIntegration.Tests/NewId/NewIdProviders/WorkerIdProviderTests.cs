using ViciOne.ServiceBus.NewIdProviders;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.LocalIntegration.Tests.NewId.NewIdProviders;

public sealed class WorkerIdProviderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-WORKER-ID", "network-adapter")]
    public void NetworkAddressProvider_ReturnsOneSixBytePhysicalAddress()
    {
        var provider = new NetworkAddressWorkerIdProvider();

        var workerId = provider.GetWorkerId(0);

        Assert.Equal(6, workerId.Length);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-WORKER-ID", "host-name-hash")]
    public void HostNameHashProvider_ReturnsOneStableSixByteValue()
    {
        var provider = new HostNameHashWorkerIdProvider();

        var first = provider.GetWorkerId(0);
        var second = provider.GetWorkerId(0);

        Assert.Equal(6, first.Length);
        Assert.Equal(first, second);
    }
}
