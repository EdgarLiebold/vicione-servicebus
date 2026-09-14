using System.Reflection;
using ViciOne.ServiceBus.Events.Receiving;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Events;

public sealed class ReceiveLifecycleEventTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EVENT-SNAPSHOTS", "receive-transport-complete-ready-and-faulted")]
    public void ReceiveTransportEvents_ProjectEveryConstructorValueExactly()
    {
        var address = new Uri("loopback://localhost/events");
        var failure = new InvalidOperationException("transport failed");
        var metrics = new DeliveryMetrics(47, 6);

        ReceiveTransportCompleted completed = new ReceiveTransportCompletedEvent(address, metrics);
        ReceiveTransportReady started = new ReceiveTransportReadyEvent(address);
        ReceiveTransportReady attached = new ReceiveTransportReadyEvent(address, isStarted: false);
        ReceiveTransportFaulted recoverable = new ReceiveTransportFaultedEvent(address, failure, isTerminal: false);
        ReceiveTransportFaulted terminal = new ReceiveTransportFaultedEvent(address, failure, isTerminal: true);

        Assert.Equal(address, completed.InputAddress);
        Assert.Equal(47, completed.DeliveryCount);
        Assert.Equal(6, completed.MaxConcurrentDeliveryCount);
        Assert.Equal(address, started.InputAddress);
        Assert.True(started.IsStarted);
        Assert.Equal(address, attached.InputAddress);
        Assert.False(attached.IsStarted);
        Assert.Equal(address, recoverable.InputAddress);
        Assert.Same(failure, recoverable.Exception);
        Assert.False(recoverable.IsTerminal);
        Assert.Equal(address, terminal.InputAddress);
        Assert.True(terminal.IsTerminal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENT-SNAPSHOTS", "receive-endpoint-projection-matrix")]
    public void ReceiveEndpointEvents_PreserveTheEndpointAndUnderlyingTransportSnapshot()
    {
        var address = new Uri("loopback://localhost/endpoint-events");
        IReceiveEndpoint endpoint = DispatchProxy.Create<IReceiveEndpoint, UnusedProxy>();
        var failure = new InvalidOperationException("endpoint transport failed");
        ReceiveTransportCompleted completed = new ReceiveTransportCompletedEvent(address, new DeliveryMetrics(91, 8));
        ReceiveTransportFaulted faulted = new ReceiveTransportFaultedEvent(address, failure, isTerminal: true);

        ReceiveEndpointCompleted endpointCompleted = new ReceiveEndpointCompletedEvent(completed, endpoint);
        ReceiveEndpointFaulted endpointFaulted = new ReceiveEndpointFaultedEvent(faulted, endpoint);
        ReceiveEndpointReady endpointReady = new ReceiveEndpointReadyEvent(address, endpoint, isStarted: false);
        ReceiveEndpointStopping endpointStopping = new ReceiveEndpointStoppingEvent(address, endpoint, removed: true);

        Assert.Equal(address, endpointCompleted.InputAddress);
        Assert.Equal(91, endpointCompleted.DeliveryCount);
        Assert.Equal(8, endpointCompleted.MaxConcurrentDeliveryCount);
        Assert.Same(endpoint, endpointCompleted.ReceiveEndpoint);
        Assert.Equal(address, endpointFaulted.InputAddress);
        Assert.Same(failure, endpointFaulted.Exception);
        Assert.True(endpointFaulted.IsTerminal);
        Assert.Same(endpoint, endpointFaulted.ReceiveEndpoint);
        Assert.Equal(address, endpointReady.InputAddress);
        Assert.False(endpointReady.IsStarted);
        Assert.Same(endpoint, endpointReady.ReceiveEndpoint);
        Assert.Equal(address, endpointStopping.InputAddress);
        Assert.True(endpointStopping.Removed);
        Assert.Same(endpoint, endpointStopping.ReceiveEndpoint);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENT-SNAPSHOTS", "completed-transport-captures-final-metrics")]
    public void ReceiveTransportCompletedEvent_CapturesMetricsAtConstruction()
    {
        var metrics = new MutableDeliveryMetrics
        {
            DeliveryCount = 37,
            MaxConcurrentDeliveryCount = 4,
        };
        var completed = new ReceiveTransportCompletedEvent(new Uri("loopback://localhost/metric-snapshot"), metrics);

        metrics.DeliveryCount = 99;
        metrics.MaxConcurrentDeliveryCount = 12;

        Assert.Equal(37, completed.DeliveryCount);
        Assert.Equal(4, completed.MaxConcurrentDeliveryCount);
    }

    [Theory]
    [InlineData(-1L, 0)]
    [InlineData(0L, -1)]
    [InlineData(2L, 3)]
    [RequirementCoverage("REQ-VSB-EVENT-SNAPSHOTS", "completed-transport-rejects-impossible-metrics")]
    public void ReceiveTransportCompletedEvent_RejectsImpossibleFinalMetrics(long deliveryCount, int maximumConcurrency)
    {
        var metrics = new DeliveryMetrics(deliveryCount, maximumConcurrency);

        Assert.Equal(
            "metrics",
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReceiveTransportCompletedEvent(
                new Uri("loopback://localhost/invalid-metrics"),
                metrics)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EVENT-SNAPSHOTS", "receive-lifecycle-required-arguments")]
    public void ReceiveLifecycleEvents_RejectEveryMissingRequiredArgument()
    {
        var address = new Uri("loopback://localhost/required-events");
        IReceiveEndpoint endpoint = DispatchProxy.Create<IReceiveEndpoint, UnusedProxy>();
        var failure = new InvalidOperationException("failure");
        ReceiveTransportCompleted completed = new ReceiveTransportCompletedEvent(address, new DeliveryMetrics(1, 1));
        ReceiveTransportFaulted faulted = new ReceiveTransportFaultedEvent(address, failure, false);

        Assert.Equal("inputAddress", Assert.Throws<ArgumentNullException>(() =>
            new ReceiveTransportCompletedEvent(null!, new DeliveryMetrics(1, 1))).ParamName);
        Assert.Equal("metrics", Assert.Throws<ArgumentNullException>(() =>
            new ReceiveTransportCompletedEvent(address, null!)).ParamName);
        Assert.Equal("inputAddress", Assert.Throws<ArgumentNullException>(() =>
            new ReceiveTransportReadyEvent(null!)).ParamName);
        Assert.Equal("inputAddress", Assert.Throws<ArgumentNullException>(() =>
            new ReceiveTransportFaultedEvent(null!, failure, false)).ParamName);
        Assert.Equal("exception", Assert.Throws<ArgumentNullException>(() =>
            new ReceiveTransportFaultedEvent(address, null!, false)).ParamName);
        Assert.Equal("completed", Assert.Throws<ArgumentNullException>(() =>
            new ReceiveEndpointCompletedEvent(null!, endpoint)).ParamName);
        Assert.Equal("receiveEndpoint", Assert.Throws<ArgumentNullException>(() =>
            new ReceiveEndpointCompletedEvent(completed, null!)).ParamName);
        Assert.Equal("faulted", Assert.Throws<ArgumentNullException>(() =>
            new ReceiveEndpointFaultedEvent(null!, endpoint)).ParamName);
        Assert.Equal("receiveEndpoint", Assert.Throws<ArgumentNullException>(() =>
            new ReceiveEndpointFaultedEvent(faulted, null!)).ParamName);
        Assert.Equal("inputAddress", Assert.Throws<ArgumentNullException>(() =>
            new ReceiveEndpointReadyEvent(null!, endpoint, true)).ParamName);
        Assert.Equal("receiveEndpoint", Assert.Throws<ArgumentNullException>(() =>
            new ReceiveEndpointReadyEvent(address, null!, true)).ParamName);
        Assert.Equal("inputAddress", Assert.Throws<ArgumentNullException>(() =>
            new ReceiveEndpointStoppingEvent(null!, endpoint, false)).ParamName);
        Assert.Equal("receiveEndpoint", Assert.Throws<ArgumentNullException>(() =>
            new ReceiveEndpointStoppingEvent(address, null!, false)).ParamName);
    }

    private sealed record DeliveryMetrics(long DeliveryCount, int MaxConcurrentDeliveryCount) : IDeliveryMetrics;

    private sealed class MutableDeliveryMetrics : IDeliveryMetrics
    {
        public long DeliveryCount { get; set; }
        public int MaxConcurrentDeliveryCount { get; set; }
    }

    private class UnusedProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException("The event projection test must not invoke the endpoint.");
    }
}
