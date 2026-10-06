using System.Collections.Concurrent;
using System.IO;
using System.Runtime.ExceptionServices;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Util;

public sealed class ActiveRequestTimerDisposalTests
{
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-CANCELLATION", "timer-dispose-fault-preserves-owner-accounting")]
    public async Task Dispose_TimerFailureRetainsItsCauseAndReleasesOwnerCapacityAsync(bool failTimerDisposal)
    {
        TimeSpan grace = TimeSpan.FromMinutes(1);
        var clock = new ControlledTimeProvider();
        var algorithm = new RequestRateAlgorithm(new RequestRateAlgorithmOptions
        {
            PrefetchCount = 1,
            RequestResultLimit = 1,
            ConcurrentResultLimit = 1,
            RequestCancellationTimeout = grace,
        }, clock);
        using var caller = new CancellationTokenSource();
        using var successorCaller = new CancellationTokenSource();
        Task<ActiveRequest>? admission = null;
        Task<ActiveRequest>? successorAdmission = null;
        Exception? primary = null;
        try
        {
            Assert.Empty(clock.Timers);
            admission = algorithm.BeginRequestAsync(caller.Token);
            ActiveRequest request = await admission.WaitAsync(CompletionTimeout, CancellationToken.None);
            Assert.Equal(1, algorithm.ActiveRequestCount);
            Assert.Equal(1, request.ResultLimit);

            caller.Cancel();
            ControlledTimer timer = Assert.Single(clock.Timers);
            Assert.Same(request, timer.State);
            Assert.NotNull(timer.Callback);
            Assert.Equal(grace, timer.DueTime);
            Assert.Equal(Timeout.InfiniteTimeSpan, timer.Period);
            Assert.False(request.CancellationToken.IsCancellationRequested);
            Assert.Equal(0, timer.DisposeCalls);
            timer.FailDisposal = failTimerDisposal;

            Exception? disposalFailure = Record.Exception(request.Dispose);
            if (failTimerDisposal)
                Assert.Same(timer.Failure, disposalFailure);
            else
                Assert.Null(disposalFailure);
            Assert.Equal(1, timer.DisposeCalls);
            Assert.Equal(0, algorithm.ActiveRequestCount);

            Assert.Null(Record.Exception(request.Dispose));
            Assert.Equal(0, algorithm.ActiveRequestCount);
            Assert.Equal(1, timer.DisposeCalls);

            successorAdmission = algorithm.BeginRequestAsync(successorCaller.Token);
            ActiveRequest successor = await successorAdmission.WaitAsync(CompletionTimeout, CancellationToken.None);
            Assert.Equal(1, successor.ResultLimit);
            Assert.Equal(1, algorithm.ActiveRequestCount);
            successor.Dispose();
            Assert.Equal(0, algorithm.ActiveRequestCount);
            Assert.Single(clock.Timers);
        }
        catch (Exception exception)
        {
            primary = exception;
            throw;
        }
        finally
        {
            var failures = new List<Exception>();
            clock.DisableDisposalFailures();
            Attempt(caller.Cancel, failures);
            Attempt(successorCaller.Cancel, failures);
            Attempt(algorithm.Dispose, failures);
            await ObserveAndRetireAsync(admission, caller.IsCancellationRequested, primary, failures);
            await ObserveAndRetireAsync(successorAdmission, successorCaller.IsCancellationRequested, primary, failures);
            foreach (ControlledTimer timer in clock.Timers)
            {
                Attempt(timer.Dispose, failures);
                if (!timer.Retired)
                    failures.Add(new InvalidOperationException("The controlled timer was not retired."));
            }

            if (failures.Count != 0)
            {
                if (primary != null)
                    failures.Insert(0, primary);
                if (failures.Count == 1)
                    ExceptionDispatchInfo.Capture(failures[0]).Throw();
                throw new AggregateException("Active request test cleanup failed.", failures);
            }
        }
    }

    private static async Task ObserveAndRetireAsync(Task<ActiveRequest>? admission,
        bool callerCanceled, Exception? primary, List<Exception> failures)
    {
        if (admission == null)
            return;

        ActiveRequest? request = null;
        try
        {
            request = await admission.WaitAsync(CompletionTimeout, CancellationToken.None);
        }
        catch (OperationCanceledException) when (admission.IsCanceled && callerCanceled)
        {
        }
        catch (Exception exception)
        {
            if (!ReferenceEquals(primary, exception))
                failures.Add(exception);
        }

        if (request != null)
            Attempt(request.Dispose, failures);
    }

    private static void Attempt(Action action, List<Exception> failures)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }
    }

    private sealed class ControlledTimeProvider : TimeProvider
    {
        private readonly ConcurrentQueue<ControlledTimer> _timers = new();

        public ControlledTimer[] Timers => _timers.ToArray();

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ControlledTimer(callback, state, dueTime, period);
            _timers.Enqueue(timer);
            return timer;
        }

        public void DisableDisposalFailures()
        {
            foreach (ControlledTimer timer in Timers)
                timer.FailDisposal = false;
        }
    }

    private sealed class ControlledTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) : ITimer
    {
        private int _failDisposal;
        private int _disposeCalls;
        private int _retired;
        private readonly object _gate = new();
        private TimeSpan _dueTime = dueTime;
        private TimeSpan _period = period;

        public TimerCallback Callback { get; } = callback;
        public object? State { get; } = state;
        public TimeSpan DueTime { get { lock (_gate) return _dueTime; } }
        public TimeSpan Period { get { lock (_gate) return _period; } }
        public IOException Failure { get; } = new("controlled timer disposal failed");
        public int DisposeCalls => Volatile.Read(ref _disposeCalls);
        public bool Retired => Volatile.Read(ref _retired) != 0;

        public bool FailDisposal
        {
            get => Volatile.Read(ref _failDisposal) != 0;
            set => Volatile.Write(ref _failDisposal, value ? 1 : 0);
        }

        public bool Change(TimeSpan nextDueTime, TimeSpan nextPeriod)
        {
            lock (_gate)
            {
                if (Retired)
                    return false;
                _dueTime = nextDueTime;
                _period = nextPeriod;
                return true;
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                Interlocked.Increment(ref _disposeCalls);
                if (FailDisposal)
                    throw Failure;
                Interlocked.Exchange(ref _retired, 1);
            }
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
