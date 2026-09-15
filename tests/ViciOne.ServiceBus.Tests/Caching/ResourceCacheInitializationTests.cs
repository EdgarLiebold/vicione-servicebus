using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Caching;

public sealed class ResourceCacheInitializationTests
{
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-CACHE-DISPOSAL", "timer-construction-fault-releases-all-allocated-ownership")]
    public void TimerCreationFailure_ReleasesAllocatedOwnershipAndPreservesTheOriginalException(bool linkLifetimeToken)
    {
        using var lifetime = new CancellationTokenSource();
        var expected = new InvalidOperationException("Cleanup timer creation failed.");
        var time = new FaultingTimerProvider(expected);
        var options = new ResourceCacheOptions(
            timeProvider: time,
            lifetimeCancellationToken: linkLifetimeToken ? lifetime.Token : CancellationToken.None);

        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(() => new ResourceCache<object>(options));
        var owner = Assert.IsType<ResourceCache<object>>(time.CallbackTarget);
        CancellationTokenSource cancellation = ReadOwnedField<CancellationTokenSource>(owner, "_lifetimeCancellationSource");
        SemaphoreSlim observerGate = ReadOwnedField<SemaphoreSlim>(owner, "_observerDispatchGate");

        try
        {
            Assert.Same(expected, actual);
            Assert.Equal(1, time.CreateTimerCount);
            Assert.Throws<ObjectDisposedException>(() => cancellation.Token);
            Assert.Throws<ObjectDisposedException>(() => observerGate.Wait(0, TestContext.Current.CancellationToken));
            Assert.False(lifetime.IsCancellationRequested);
            lifetime.Cancel();
            Assert.True(lifetime.IsCancellationRequested);
        }
        finally
        {
            observerGate.Dispose();
            cancellation.Dispose();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-DISPOSAL", "successful-construction-forwards-timer-policy-and-releases-timer-once")]
    public async Task SuccessfulConstruction_ForwardsCleanupPolicyAndDisposesItsTimerExactlyOnceAsync()
    {
        var time = new RecordingTimerProvider();
        var interval = TimeSpan.FromSeconds(7);
        var cache = new ResourceCache<object>(new ResourceCacheOptions(timeProvider: time, cleanupInterval: interval));
        try
        {
            RecordingTimer timer = Assert.IsType<RecordingTimer>(time.Timer);

            Assert.Equal(interval, time.DueTime);
            Assert.Equal(interval, time.Period);
            Assert.Equal(1, time.CreateTimerCount);
            Assert.Equal(0, timer.DisposeCount);
            Assert.Equal(default, cache.Statistics);

            Task first = cache.DisposeAsync().AsTask();
            Task second = cache.DisposeAsync().AsTask();

            Assert.Same(first, second);
            await first.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            await second.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            Assert.Equal(1, timer.DisposeCount);
            Assert.Throws<ObjectDisposedException>(() => cache.AddIndex("late", value => value));
        }
        finally
        {
            await cache.DisposeAsync().AsTask().WaitAsync(OperationTimeout, CancellationToken.None);
        }
    }

    // The timer callback exposes the failed constructor's owner. Private field inspection
    // checks disposal of allocations that cannot be reached through a successfully returned cache.
    private static TField ReadOwnedField<TField>(ResourceCache<object> owner, string name)
        where TField : class
    {
        FieldInfo field = Assert.IsAssignableFrom<FieldInfo>(typeof(ResourceCache<object>).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic));
        return Assert.IsAssignableFrom<TField>(field.GetValue(owner));
    }

    private sealed class FaultingTimerProvider(InvalidOperationException exception) : TimeProvider
    {
        public object? CallbackTarget { get; private set; }
        public int CreateTimerCount { get; private set; }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            CallbackTarget = callback.Target;
            CreateTimerCount++;
            throw exception;
        }
    }

    private sealed class RecordingTimerProvider : TimeProvider
    {
        private readonly FakeTimeProvider _time = new(DateTimeOffset.UnixEpoch);

        public RecordingTimer? Timer { get; private set; }
        public TimeSpan DueTime { get; private set; }
        public TimeSpan Period { get; private set; }
        public int CreateTimerCount { get; private set; }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            DueTime = dueTime;
            Period = period;
            CreateTimerCount++;
            Timer = new RecordingTimer(_time.CreateTimer(callback, state, dueTime, period));
            return Timer;
        }
    }

    private sealed class RecordingTimer(ITimer inner) : ITimer
    {
        private int _disposeCount;

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public bool Change(TimeSpan dueTime, TimeSpan period) => inner.Change(dueTime, period);

        public void Dispose()
        {
            Interlocked.Increment(ref _disposeCount);
            inner.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            await inner.DisposeAsync();
        }
    }
}
