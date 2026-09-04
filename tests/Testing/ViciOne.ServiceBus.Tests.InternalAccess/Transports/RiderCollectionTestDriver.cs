using System.Collections.Concurrent;
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

    public Task StopAsync(CancellationToken cancellationToken = default) =>
        _collection.StopRidersAsync(cancellationToken);

    public int[] StartedGenerations => _rider.StartedGenerations;
    public int[] StoppedGenerations => _rider.StoppedGenerations;


    private sealed class TrackingRider : IRiderControl
    {
        private readonly ConcurrentQueue<int> _started = [];
        private readonly ConcurrentQueue<int> _stopped = [];
        private int _generation;

        public int[] StartedGenerations => _started.ToArray();
        public int[] StoppedGenerations => _stopped.ToArray();

        public RiderHandle Start(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested(); int generation = Interlocked.Increment(ref _generation);
            _started.Enqueue(generation);
            return new TrackingHandle(generation, _stopped);
        }

        public IEnumerable<EndpointHealthResult> CheckEndpointHealth() => [];
    }


    private sealed class TrackingHandle(int generation, ConcurrentQueue<int> stopped) : RiderHandle
    {
        private int _isStopped;

        public Task Ready => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Interlocked.Exchange(ref _isStopped, 1) == 0)
                stopped.Enqueue(generation);
            return Task.CompletedTask;
        }
    }
}
