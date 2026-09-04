using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Brokers;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.Tests;

public sealed class ActiveMqRecoveryObserverTests
{
    private const string TargetEndpoint = "recovery-input";
    private const string ForeignEndpoint = "harness-input";

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-RECOVERY", "fault-before-ready-current-watch-and-target-endpoint")]
    public async Task RecoveryStateMachine_RejectsStaleForeignAndOutOfOrderSignalsAsync()
    {
        var observer = new ReceiveEndpointRecoveryObserver(TargetEndpoint);

        await observer.FaultedAsync(new EndpointFaulted(TargetEndpoint));
        await observer.ReadyAsync(new EndpointReady(TargetEndpoint));
        Assert.Equal(new ReceiveEndpointRecoverySnapshot(false, false, false), observer.Snapshot);

        observer.Watch();
        await observer.ReadyAsync(new EndpointReady(TargetEndpoint));
        await observer.FaultedAsync(new EndpointFaulted(ForeignEndpoint));
        Assert.Equal(new ReceiveEndpointRecoverySnapshot(true, false, false), observer.Snapshot);

        await observer.FaultedAsync(new EndpointFaulted(TargetEndpoint));
        Assert.Equal(new ReceiveEndpointRecoverySnapshot(true, true, false), observer.Snapshot);

        await observer.ReadyAsync(new EndpointReady(ForeignEndpoint));
        Assert.Equal(new ReceiveEndpointRecoverySnapshot(true, true, false), observer.Snapshot);

        await observer.ReadyAsync(new EndpointReady(TargetEndpoint));
        Assert.Equal(new ReceiveEndpointRecoverySnapshot(true, true, true), observer.Snapshot);

        observer.Watch();
        await observer.ReadyAsync(new EndpointReady(TargetEndpoint));
        Assert.Equal(new ReceiveEndpointRecoverySnapshot(true, false, false), observer.Snapshot);

        await observer.FaultedAsync(new EndpointFaulted(TargetEndpoint));
        Assert.Equal(new ReceiveEndpointRecoverySnapshot(true, true, false), observer.Snapshot);
    }

    private sealed class EndpointReady(string endpoint) : ReceiveEndpointReady
    {
        public Uri InputAddress { get; } = new($"activemq://broker:61616/{Uri.EscapeDataString(endpoint)}");
        public IReceiveEndpoint ReceiveEndpoint => null!;
        public bool IsStarted => true;
    }

    private sealed class EndpointFaulted(string endpoint) : ReceiveEndpointFaulted
    {
        public Uri InputAddress { get; } = new($"activemq://broker:61616/{Uri.EscapeDataString(endpoint)}");
        public IReceiveEndpoint ReceiveEndpoint => null!;
        public Exception Exception { get; } = new InvalidOperationException("The endpoint faulted.");
        public bool IsTerminal => false;
    }
}
