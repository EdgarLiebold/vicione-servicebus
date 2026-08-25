namespace ViciOne.ServiceBus.Testing.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Internals;
    using Util;


    public class AsyncInactivityObserver :
        IDisposable,
        IInactivityObserver
    {
        readonly Lazy<Task> _inactivityTask;
        readonly TaskCompletionSource<bool> _inactivityTaskSource;
        readonly CancellationTokenSource _inactivityTokenSource;
        readonly HashSet<IInactivityObservationSource> _sources;
        readonly TimeProvider _timeProvider;
        int _disposed;

        public AsyncInactivityObserver(TimeSpan timeout, CancellationToken cancellationToken)
            : this(timeout, cancellationToken, TimeProvider.System)
        {
        }

        public AsyncInactivityObserver(TimeSpan timeout, CancellationToken cancellationToken, TimeProvider timeProvider)
        {
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _inactivityTaskSource = TaskUtil.GetTask();
            _inactivityTask = new Lazy<Task>(() => TimeoutTask(timeout, cancellationToken));

            _sources = new HashSet<IInactivityObservationSource>();
            _inactivityTokenSource = new CancellationTokenSource();
        }

        public Task InactivityTask => _inactivityTask.Value;

        public CancellationToken InactivityToken => _inactivityTokenSource.Token;

        public void Connected(IInactivityObservationSource source)
        {
            ArgumentNullException.ThrowIfNull(source);

            lock (_sources)
                _sources.Add(source);
        }

        public Task NoActivity()
        {
            return CheckSourceActivity();
        }

        public void ForceInactive()
        {
            if (Volatile.Read(ref _disposed) != 0)
                return;

            _inactivityTaskSource.TrySetResult(true);
            _inactivityTokenSource.Cancel();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            _inactivityTaskSource.TrySetCanceled();
            _inactivityTokenSource.Cancel();
            _inactivityTokenSource.Dispose();
        }

        Task<bool> CheckSourceActivity()
        {
            IInactivityObservationSource[] sources;
            lock (_sources)
                sources = _sources.ToArray();

            if (sources.All(x => x.IsInactive))
            {
                _inactivityTaskSource.TrySetResult(true);
                _inactivityTokenSource.Cancel();

                return TaskUtil.True;
            }

            return TaskUtil.False;
        }

        async Task TimeoutTask(TimeSpan timeout, CancellationToken cancellationToken)
        {
            try
            {
                // The completion may already have been forced before this task was materialized, because the task
                // is created lazily on the first access. Waiting an interval first would ignore that.
                var inActive = _inactivityTaskSource.Task.IsCompleted;
                while (!inActive)
                {
                    using var delayCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                    Task delay = Task.Delay(timeout, _timeProvider, delayCancellation.Token);

                    Task completed = await Task.WhenAny(delay, _inactivityTaskSource.Task).ConfigureAwait(false);
                    if (completed != delay)
                    {
                        // Forced while this interval was running. The interval is no longer needed, so it is
                        // cancelled instead of being left to run to its end.
                        delayCancellation.Cancel();
                        delay.IgnoreUnobservedExceptions();
                        break;
                    }

                    await delay.ConfigureAwait(false);

                    inActive = await CheckSourceActivity().ConfigureAwait(false);
                }

                await _inactivityTaskSource.Task.OrCanceled(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }
    }
}
