namespace ViciOne.ServiceBus.Util
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;


    public sealed class ActiveRequest :
        IDisposable
    {
        readonly RequestRateAlgorithm _algorithm;
        readonly CancellationTokenRegistration _registration;
        readonly CancellationTokenSource _source;
        readonly TimeProvider _timeProvider;
        readonly TimeSpan _timeout;
        ITimer? _cancelTimer;
        bool _completed;
        int _disposed;

        public ActiveRequest(RequestRateAlgorithm algorithm, int resultLimit, CancellationToken cancellationToken, TimeSpan timeout,
            TimeProvider timeProvider)
        {
            _algorithm = algorithm;
            _timeout = timeout;
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _source = new CancellationTokenSource();

            CancellationToken = _source.Token;
            ResultLimit = resultLimit;

            _registration = cancellationToken.Register(static state => ((ActiveRequest)state!).ScheduleCancellation(), this);
        }

        public CancellationToken CancellationToken { get; }
        public int ResultLimit { get; }

        public Task Complete(int count, CancellationToken cancellationToken = default)
        {
            _completed = true;

            return _algorithm.EndRequest(count, ResultLimit, cancellationToken);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            _registration.Dispose();
            _cancelTimer?.Dispose();
            _source.Dispose();

            if (_completed)
                return;

            _algorithm.CancelRequest(ResultLimit);
        }

        void ScheduleCancellation()
        {
            if (Volatile.Read(ref _disposed) != 0)
                return;

            if (_timeout <= TimeSpan.Zero)
            {
                Cancel();
                return;
            }

            var timer = _timeProvider.CreateTimer(static state => ((ActiveRequest)state!).Cancel(), this, _timeout, Timeout.InfiniteTimeSpan);
            var previous = Interlocked.CompareExchange(ref _cancelTimer, timer, null);
            if (previous != null)
                timer.Dispose();
        }

        void Cancel()
        {
            if (Volatile.Read(ref _disposed) != 0)
                return;

            try
            {
                _source.Cancel();
            }
            catch (ObjectDisposedException) when (Volatile.Read(ref _disposed) != 0)
            {
            }
        }
    }
}
