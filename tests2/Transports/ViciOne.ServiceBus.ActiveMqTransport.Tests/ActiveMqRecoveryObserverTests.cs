using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.Tests;

public sealed class ActiveMqRecoveryObserverTests
{
    private const string TargetEndpoint = "recovery-input";
    private const string ForeignEndpoint = "harness-input";

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-RECOVERY", "fault-before-ready-current-watch-and-target-endpoint")]
    public async Task RecoveryStateMachine_RejectsStaleForeignAndOutOfOrderSignals()
    {
        var observer = new RecoverySequenceObserver(TargetEndpoint);

        await observer.Faulted(new EndpointFaulted(TargetEndpoint));
        await observer.Ready(new EndpointReady(TargetEndpoint));
        Assert.Equal(new RecoverySnapshot(false, false, false), observer.Snapshot);

        observer.Watch();
        await observer.Ready(new EndpointReady(TargetEndpoint));
        await observer.Faulted(new EndpointFaulted(ForeignEndpoint));
        Assert.Equal(new RecoverySnapshot(true, false, false), observer.Snapshot);

        await observer.Faulted(new EndpointFaulted(TargetEndpoint));
        Assert.Equal(new RecoverySnapshot(true, true, false), observer.Snapshot);

        await observer.Ready(new EndpointReady(ForeignEndpoint));
        Assert.Equal(new RecoverySnapshot(true, true, false), observer.Snapshot);

        await observer.Ready(new EndpointReady(TargetEndpoint));
        Assert.Equal(new RecoverySnapshot(true, true, true), observer.Snapshot);

        observer.Watch();
        await observer.Ready(new EndpointReady(TargetEndpoint));
        Assert.Equal(new RecoverySnapshot(true, false, false), observer.Snapshot);

        await observer.Faulted(new EndpointFaulted(TargetEndpoint));
        Assert.Equal(new RecoverySnapshot(true, true, false), observer.Snapshot);
    }

    private readonly record struct RecoverySnapshot(bool Watching, bool FaultSeen, bool Recovered);

    private sealed class RecoverySequenceObserver(string endpoint) : IReceiveEndpointObserver
    {
        private readonly object _gate = new();
        private bool _faultSeen;
        private bool _recovered;
        private bool _watching;

        public RecoverySnapshot Snapshot
        {
            get
            {
                lock (_gate)
                    return new RecoverySnapshot(_watching, _faultSeen, _recovered);
            }
        }

        public void Watch()
        {
            lock (_gate)
            {
                _watching = true;
                _faultSeen = false;
                _recovered = false;
            }
        }

        public Task Ready(ReceiveEndpointReady ready)
        {
            if (!IsTarget(ready.InputAddress))
                return Task.CompletedTask;

            lock (_gate)
            {
                if (_watching && _faultSeen)
                    _recovered = true;
            }

            return Task.CompletedTask;
        }

        public Task Stopping(ReceiveEndpointStopping stopping) => Task.CompletedTask;

        public Task Completed(ReceiveEndpointCompleted completed) => Task.CompletedTask;

        public Task Faulted(ReceiveEndpointFaulted faulted)
        {
            if (!IsTarget(faulted.InputAddress))
                return Task.CompletedTask;

            lock (_gate)
            {
                if (_watching)
                    _faultSeen = true;
            }

            return Task.CompletedTask;
        }

        private bool IsTarget(Uri? address)
        {
            if (address is null)
                return false;

            string[] segments = address.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments.Length > 0
                && string.Equals(Uri.UnescapeDataString(segments[^1]), endpoint, StringComparison.Ordinal);
        }
    }

    private sealed class EndpointReady(string endpoint) : ReceiveEndpointReady
    {
        public Uri InputAddress { get; } = new($"activemq://broker/{Uri.EscapeDataString(endpoint)}");
        public IReceiveEndpoint ReceiveEndpoint => null!;
        public bool IsStarted => true;
    }

    private sealed class EndpointFaulted(string endpoint) : ReceiveEndpointFaulted
    {
        public Uri InputAddress { get; } = new($"activemq://broker/{Uri.EscapeDataString(endpoint)}");
        public IReceiveEndpoint ReceiveEndpoint => null!;
        public Exception Exception { get; } = new InvalidOperationException("The endpoint faulted.");
    }
}
