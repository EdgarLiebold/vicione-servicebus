namespace ViciOne.ServiceBus.Tests.InternalAccess.Brokers;

/// <summary>
/// Observes one named receive endpoint and exposes the ordered fault-then-ready recovery sequence.
/// Signals emitted before <see cref="Watch"/> or by another endpoint cannot satisfy the current observation.
/// </summary>
public sealed class ReceiveEndpointRecoveryObserver(string endpointName) : IReceiveEndpointObserver
{
    private readonly object _gate = new();
    private TaskCompletionSource<ReceiveEndpointFaulted> _faulted = NewSignal<ReceiveEndpointFaulted>();
    private bool _faultSeen;
    private TaskCompletionSource<ReceiveEndpointReady> _recovered = NewSignal<ReceiveEndpointReady>();
    private bool _watching;

    public ReceiveEndpointRecoverySnapshot Snapshot
    {
        get
        {
            lock (_gate)
                return new ReceiveEndpointRecoverySnapshot(_watching, _faultSeen, _recovered.Task.IsCompletedSuccessfully);
        }
    }

    public Task<ReceiveEndpointFaulted> FaultObserved => _faulted.Task;

    public Task<ReceiveEndpointReady> RecoveryObserved => _recovered.Task;

    public void Watch()
    {
        lock (_gate)
        {
            _watching = true;
            _faultSeen = false;
            _faulted = NewSignal<ReceiveEndpointFaulted>();
            _recovered = NewSignal<ReceiveEndpointReady>();
        }
    }

    public Task ReadyAsync(ReceiveEndpointReady ready)
    {
        if (!IsTarget(ready.InputAddress))
            return Task.CompletedTask;

        lock (_gate)
        {
            if (_watching && _faultSeen)
                _recovered.TrySetResult(ready);
        }

        return Task.CompletedTask;
    }

    public Task StoppingAsync(ReceiveEndpointStopping stopping) => Task.CompletedTask;

    public Task CompletedAsync(ReceiveEndpointCompleted completed) => Task.CompletedTask;

    public Task FaultedAsync(ReceiveEndpointFaulted faulted)
    {
        if (!IsTarget(faulted.InputAddress))
            return Task.CompletedTask;

        lock (_gate)
        {
            if (_watching)
            {
                _faultSeen = true;
                _faulted.TrySetResult(faulted);
            }
        }

        return Task.CompletedTask;
    }

    private bool IsTarget(Uri? address)
    {
        if (address is null)
            return false;

        string[] segments = address.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 0
            && string.Equals(Uri.UnescapeDataString(segments[^1]), endpointName, StringComparison.Ordinal);
    }

    private static TaskCompletionSource<T> NewSignal<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}

public readonly record struct ReceiveEndpointRecoverySnapshot(bool Watching, bool FaultSeen, bool Recovered);
