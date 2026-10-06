using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class BusObserverDisposalFailureBoundaryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "observer-dispose-failure-still-retires-base-scopes")]
    public async Task DisposeAsync_ObserverTimerFailureStillRetiresAllOwnedScopesAsync(bool timerThrows)
    {
        var uniqueFailure = new IOException("unique actual observer timer disposal failure");
        var clock = new TimerLedger(uniqueFailure);
        var harness = new ForwardingHarness(clock, $"observer-disposal-{Guid.NewGuid():N}")
        {
            TestTimeout = TimeSpan.FromHours(1),
            TestInactivityTimeout = TimeSpan.FromMinutes(7),
        };
        ILogContext? previous = LogContext.Current;
        Task? start = null;
        Task? dispose = null;
        CancellationToken testToken = default;
        CancellationToken inactivityToken = default;
        try
        {
            start = harness.StartAsync(TestContext.Current.CancellationToken);
            await start.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
            Assert.NotNull(harness.Actual);
            Assert.NotNull(harness.Proxy);
            Assert.Empty(harness.Proxy.StopTasks());
            testToken = harness.TestCancellationToken;
            inactivityToken = harness.InactivityToken;
            Assert.True(testToken.CanBeCanceled);
            Assert.False(testToken.IsCancellationRequested);
            Assert.True(inactivityToken.CanBeCanceled);
            Assert.False(inactivityToken.IsCancellationRequested);
            TimerLedger.TrackedTimer[] observers = clock.Timers()
                .Where(timer => timer.DueTime == harness.TestInactivityTimeout && timer.Period == Timeout.InfiniteTimeSpan)
                .ToArray();
            Assert.Equal(3, observers.Length);
            Assert.All(observers, timer => Assert.False(timer.InnerReleased));
            clock.Arm(observers, timerThrows);

            dispose = harness.DisposeAsync().AsTask();
            Exception? failure = await Record.ExceptionAsync(() => dispose.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None));
            Task rawStop = Assert.Single(harness.Proxy.StopTasks());
            await rawStop.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
            Assert.True(rawStop.IsCompletedSuccessfully);
            Assert.Throws<InvalidOperationException>(() => harness.BusControl);
            Assert.Equal(timerThrows ? 1 : 0, clock.ThrowCount);
            if (timerThrows)
            {
                Assert.Same(uniqueFailure, clock.ThrownFailure);
                Assert.NotNull(clock.ThrowingTimer);
                Assert.Contains(clock.ThrowingTimer, observers);
                Assert.True(clock.ThrowingTimer.InnerReleased);
                Assert.Equal(1, clock.ThrowingTimer.DisposeCalls);
                Assert.Same(uniqueFailure, failure);
            }
            else
                Assert.Null(failure);

            // This observation occurs before every public fallback and timer release.
            Assert.True(testToken.IsCancellationRequested);

            Assert.True(inactivityToken.IsCancellationRequested);
            Assert.All(observers, timer =>
            {
                Assert.Equal(1, timer.DisposeCalls);
                Assert.True(timer.InnerReleased);
            });
            Assert.All(clock.Timers(), timer => Assert.True(timer.InnerReleased));
            Assert.Throws<ObjectDisposedException>(() => harness.TestCancellationToken);
        }
        finally
        {
            var failures = new List<Exception>();
            clock.DisableFailure();
            try
            {
                if (start is not null)
                    await ObserveAsync(start, null, failures);
                if (dispose is null)
                {
                    try { dispose = harness.DisposeAsync().AsTask(); }
                    catch (Exception failure) { failures.Add(failure); }
                }
                if (dispose is not null)
                    await ObserveAsync(dispose, clock.ThrownFailure, failures);
                if (harness.Proxy is not null)
                    foreach (Task task in harness.Proxy.StopTasks())
                        await ObserveAsync(task, null, failures);
                if (harness.Actual is not null)
                {
                    Task? fallbackStop = null;
                    try { fallbackStop = harness.Actual.StopAsync(CancellationToken.None); }
                    catch (Exception failure) { failures.Add(failure); }
                    if (fallbackStop is not null)
                        await ObserveAsync(fallbackStop, null, failures);
                }
                // Repeat Dispose cannot enter a base scope skipped by the original observer failure.
                if (testToken.CanBeCanceled && !testToken.IsCancellationRequested)
                {
                    try { harness.Cancel(); }
                    catch (Exception failure) { failures.Add(failure); }
                }
                if (inactivityToken.CanBeCanceled && !inactivityToken.IsCancellationRequested)
                {
                    try { harness.ForceInactive(); }
                    catch (Exception failure) { failures.Add(failure); }
                }
            }
            finally
            {
                try
                {
                    foreach (TimerLedger.TrackedTimer timer in clock.Timers())
                    {
                        try { timer.Dispose(); }
                        catch (Exception failure) { failures.Add(failure); }
                    }
                }
                finally { LogContext.Current = previous; }
            }
            if (failures.Count == 1)
                ExceptionDispatchInfo.Capture(failures[0]).Throw();
            if (failures.Count > 1)
                throw new AggregateException("Bus observer-disposal fixture cleanup failed.", failures);
        }
    }

    static async Task ObserveAsync(Task task, Exception? expected, List<Exception> failures)
    {
        try { await task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None); }
        catch (Exception failure) when (task.IsCompleted && expected is not null && ReferenceEquals(failure, expected)) { }
        catch (Exception failure) { failures.Add(failure); }
    }

    sealed class ForwardingHarness(TimeProvider clock, string virtualHost) : InMemoryTestHarness(clock, virtualHost)
    {
        public IBusControl? Actual { get; private set; }
        public BusProxy? Proxy { get; private set; }
        protected override async Task<IBusControl> CreateBusAsync(CancellationToken cancellationToken)
        {
            Actual = await base.CreateBusAsync(cancellationToken);
            IBusControl forwarded = DispatchProxy.Create<IBusControl, BusProxy>();
            Proxy = (BusProxy)(object)forwarded;
            Proxy.Actual = Actual;
            return forwarded;
        }
    }

    public class BusProxy : DispatchProxy
    {
        readonly object _sync = new();
        readonly List<Task> _stopTasks = new();
        public IBusControl Actual { get; set; } = null!;
        public Task[] StopTasks() { lock (_sync) return _stopTasks.ToArray(); }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is null)
                throw new InvalidOperationException("Missing public bus invocation.");
            object? result;
            try { result = targetMethod.Invoke(Actual, args); }
            catch (TargetInvocationException failure) when (failure.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(failure.InnerException).Throw();
                throw;
            }
            if (targetMethod.Name == nameof(IBusControl.StopAsync))
            {
                Task task = Assert.IsAssignableFrom<Task>(result);
                lock (_sync) _stopTasks.Add(task);
            }
            return result;
        }
    }

    sealed class TimerLedger(IOException failure) : TimeProvider
    {
        readonly FakeTimeProvider _inner = new();
        readonly object _sync = new();
        readonly List<TrackedTimer> _timers = new();
        int _failureEnabled;
        int _throwCount;
        public int ThrowCount => Volatile.Read(ref _throwCount);
        public IOException? ThrownFailure { get; private set; }
        public TrackedTimer? ThrowingTimer { get; private set; }
        public override DateTimeOffset GetUtcNow() => _inner.GetUtcNow();
        public override TimeZoneInfo LocalTimeZone => _inner.LocalTimeZone;
        public override long TimestampFrequency => _inner.TimestampFrequency;
        public override long GetTimestamp() => _inner.GetTimestamp();

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var tracked = new TrackedTimer(this, _inner.CreateTimer(callback, state, dueTime, period), dueTime, period);
            lock (_sync) _timers.Add(tracked);
            return tracked;
        }
        public TrackedTimer[] Timers() { lock (_sync) return _timers.ToArray(); }
        public void Arm(TrackedTimer[] timers, bool enabled)
        {
            foreach (TrackedTimer timer in timers)
                timer.Selected = true;
            Volatile.Write(ref _failureEnabled, enabled ? 1 : 0);
        }
        public void DisableFailure() => Volatile.Write(ref _failureEnabled, 0);
        void AfterInnerRelease(TrackedTimer timer)
        {
            if (timer.Selected && Volatile.Read(ref _failureEnabled) != 0
                && Interlocked.CompareExchange(ref _throwCount, 1, 0) == 0)
            {
                ThrowingTimer = timer;
                ThrownFailure = failure;
                throw failure;
            }
        }

        public sealed class TrackedTimer(TimerLedger ledger, ITimer inner, TimeSpan dueTime, TimeSpan period) : ITimer
        {
            readonly object _sync = new();
            int _disposeCalls;
            int _selected;
            bool _innerReleased;
            public TimeSpan DueTime { get; } = dueTime;
            public TimeSpan Period { get; } = period;
            public bool Selected
            {
                get => Volatile.Read(ref _selected) != 0;
                set => Volatile.Write(ref _selected, value ? 1 : 0);
            }
            public int DisposeCalls => Volatile.Read(ref _disposeCalls);
            public bool InnerReleased { get { lock (_sync) return _innerReleased; } }
            public bool Change(TimeSpan nextDueTime, TimeSpan nextPeriod)
            {
                lock (_sync) return inner.Change(nextDueTime, nextPeriod);
            }
            public void Dispose()
            {
                Interlocked.Increment(ref _disposeCalls);
                lock (_sync)
                {
                    if (!_innerReleased)
                    {
                        inner.Dispose();
                        _innerReleased = true;
                    }
                }
                ledger.AfterInnerRelease(this);
            }
            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
