using System.Collections.Concurrent;
using System.Linq;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Tests.InternalAccess.Transports;

public sealed class RiderCollectionTestDriver
{
    private readonly RiderCollection _collection = new();
    private readonly TrackingRider _rider = new();

    public RiderCollectionTestDriver()
    {
        _collection.Add("tracked", _rider);
    }

    public int Start() => _collection.StartRiders().Length;

    public IRider Get(string name) => _collection.Get(name);

    public async Task<int> StartConcurrentlyAsync(int callerCount, CancellationToken cancellationToken = default)
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var readyCount = 0;
        Task<HostRiderHandle[]>[] starts = Enumerable.Range(0, callerCount)
            .Select(_ => Task.Run(async () =>
            {
                if (Interlocked.Increment(ref readyCount) == callerCount)
                    ready.TrySetResult();

                await release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                return _collection.StartRiders(cancellationToken);
            }, cancellationToken))
            .ToArray();

        await ready.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        release.TrySetResult();
        HostRiderHandle[][] handles = await Task.WhenAll(starts).ConfigureAwait(false);
        return handles.Sum(x => x.Length);
    }

    public async Task StopHandleConcurrentlyAsync(int callerCount, CancellationToken cancellationToken = default)
    {
        HostRiderHandle handle = _collection.StartRider("tracked", cancellationToken);
        await Task.WhenAll(Enumerable.Range(0, callerCount).Select(_ => handle.StopAsync(cancellationToken))).ConfigureAwait(false);
    }

    public async Task<Exception> StopHandleWithSynchronousFailureThenRetryAsync(CancellationToken cancellationToken = default)
    {
        _rider.FailNextStop();
        HostRiderHandle handle = _collection.StartRider("tracked", cancellationToken);

        Exception failure;
        try
        {
            await handle.StopAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException("The scripted rider stop was expected to fail.");
        }
        catch (ScriptedStopException exception)
        {
            failure = exception;
        }

        await handle.StopAsync(cancellationToken).ConfigureAwait(false);
        return failure;
    }

    public Task StopAsync(CancellationToken cancellationToken = default) =>
        _collection.StopRidersAsync(cancellationToken);

    public int[] StartedGenerations => _rider.StartedGenerations;
    public int[] StoppedGenerations => _rider.StoppedGenerations;
    public int StopInvocationCount => _rider.StopInvocationCount;
    public IRider TrackedRider => _rider;


    private sealed class TrackingRider : IRiderControl
    {
        private readonly ConcurrentQueue<int> _started = [];
        private readonly ConcurrentQueue<int> _stopped = [];
        private int _generation;
        private int _remainingStopFailures;
        private TrackingHandle? _latestHandle;

        public int[] StartedGenerations => _started.ToArray();
        public int[] StoppedGenerations => _stopped.ToArray();
        public int StopInvocationCount => _latestHandle?.StopInvocationCount ?? 0;

        public RiderHandle Start(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int generation = Interlocked.Increment(ref _generation);
            _started.Enqueue(generation);
            var handle = new TrackingHandle(generation, _stopped, TakeStopFailure);
            _latestHandle = handle;
            return handle;
        }

        public IEnumerable<EndpointHealthResult> CheckEndpointHealth() => [];

        public void FailNextStop() => Interlocked.Exchange(ref _remainingStopFailures, 1);

        Exception? TakeStopFailure() => Interlocked.Exchange(ref _remainingStopFailures, 0) == 1
            ? new ScriptedStopException()
            : null;
    }


    private sealed class TrackingHandle(
        int generation,
        ConcurrentQueue<int> stopped,
        Func<Exception?> takeStopFailure) : RiderHandle
    {
        private int _isStopped;
        private int _stopInvocationCount;

        public int StopInvocationCount => Volatile.Read(ref _stopInvocationCount);

        public Task Ready => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _stopInvocationCount);
            if (takeStopFailure() is { } exception)
                return Task.FromException(exception);

            if (Interlocked.Exchange(ref _isStopped, 1) == 0)
                stopped.Enqueue(generation);
            return Task.CompletedTask;
        }
    }

    private sealed class ScriptedStopException : Exception;
}
