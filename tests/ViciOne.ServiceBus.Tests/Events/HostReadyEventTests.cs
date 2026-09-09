using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Events;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Events;

public sealed class HostReadyEventTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-READINESS", "immutable-endpoint-and-rider-snapshots")]
    public void Constructor_CopiesEndpointAndRiderCollectionsIntoReadOnlySnapshots()
    {
        ReceiveEndpointReady firstEndpoint = PassiveProxy.Create<ReceiveEndpointReady>();
        ReceiveEndpointReady replacementEndpoint = PassiveProxy.Create<ReceiveEndpointReady>();
        RiderReady firstRider = PassiveProxy.Create<RiderReady>();
        RiderReady replacementRider = PassiveProxy.Create<RiderReady>();
        ReceiveEndpointReady[] endpoints = [firstEndpoint];
        RiderReady[] riders = [firstRider];

        HostReady result = HostReadyEventTestDriver.Create(
            new Uri("loopback://host-readiness/bus"),
            endpoints,
            riders);
        endpoints[0] = replacementEndpoint;
        riders[0] = replacementRider;

        Assert.Same(firstEndpoint, Assert.Single(result.ReceiveEndpoints));
        Assert.Same(firstRider, Assert.Single(result.Riders));
        IList<ReceiveEndpointReady> endpointMutation =
            Assert.IsAssignableFrom<IList<ReceiveEndpointReady>>(result.ReceiveEndpoints);
        IList<RiderReady> riderMutation = Assert.IsAssignableFrom<IList<RiderReady>>(result.Riders);
        Assert.True(endpointMutation.IsReadOnly);
        Assert.True(riderMutation.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => endpointMutation[0] = replacementEndpoint);
        Assert.Throws<NotSupportedException>(() => riderMutation[0] = replacementRider);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-READINESS", "required-owner-collections-and-elements")]
    public void Constructor_RejectsMissingAddressCollectionsAndElements()
    {
        ReceiveEndpointReady endpoint = PassiveProxy.Create<ReceiveEndpointReady>();
        RiderReady rider = PassiveProxy.Create<RiderReady>();

        AssertParameter("hostAddress", () => HostReadyEventTestDriver.Create(null!, [endpoint], [rider]));
        AssertParameter("receiveEndpoints", () => HostReadyEventTestDriver.Create(
            new Uri("loopback://host-readiness/bus"),
            null!,
            [rider]));
        AssertParameter("riders", () => HostReadyEventTestDriver.Create(
            new Uri("loopback://host-readiness/bus"),
            [endpoint],
            null!));
        Assert.Equal(
            "receiveEndpoints",
            Assert.Throws<ArgumentException>(() => HostReadyEventTestDriver.Create(
                new Uri("loopback://host-readiness/bus"),
                [null!],
                [rider])).ParamName);
        Assert.Equal(
            "riders",
            Assert.Throws<ArgumentException>(() => HostReadyEventTestDriver.Create(
                new Uri("loopback://host-readiness/bus"),
                [endpoint],
                [null!])).ParamName);
    }

    private static void AssertParameter(string expected, Func<object?> action)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => action());
        Assert.Equal(expected, exception.ParamName);
    }

    public class PassiveProxy : DispatchProxy
    {
        public static T Create<T>() where T : class => DispatchProxy.Create<T, PassiveProxy>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"Unexpected readiness member: {targetMethod?.Name}.");
    }
}
