using System.Reflection;
using ViciOne.ServiceBus.Events.Readiness;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Events;

public sealed class BusReadyEventTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-READINESS", "host-and-bus-identity")]
    public void Constructor_PreservesTheExactHostAndBus()
    {
        HostReady host = DispatchProxy.Create<HostReady, UnusedProxy>();
        IBus bus = DispatchProxy.Create<IBus, UnusedProxy>();

        var ready = new BusReadyEvent(host, bus);

        Assert.Same(host, ready.Host);
        Assert.Same(bus, ready.Bus);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-READINESS", "required-host-and-bus")]
    public void Constructor_RejectsEveryMissingRequiredOwner()
    {
        HostReady host = DispatchProxy.Create<HostReady, UnusedProxy>();
        IBus bus = DispatchProxy.Create<IBus, UnusedProxy>();

        Assert.Equal("host", Assert.Throws<ArgumentNullException>(() => new BusReadyEvent(null!, bus)).ParamName);
        Assert.Equal("bus", Assert.Throws<ArgumentNullException>(() => new BusReadyEvent(host, null!)).ParamName);
    }

    private class UnusedProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The readiness test must not invoke {targetMethod?.Name}.");
    }
}
