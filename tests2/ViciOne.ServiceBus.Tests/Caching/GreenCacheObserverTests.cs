using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Caching.Internals;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Caching;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Caching;

public sealed class GreenCacheObserverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-OBSERVER", "add-fanout-completes-before-rethrow")]
    public async Task ThrowingObserverBeforeIndex_AddsToTheIndexBeforeRethrowingTheExactFailure()
    {
        var cache = new GreenCache<CacheValue>();
        var failure = new InvalidOperationException("Observer failure.");
        using ConnectHandle throwingConnection = cache.Connect(
            new SelectiveThrowingObserver<CacheValue>(addedFailure: failure));
        IIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);
        var value = new CacheValue("key-0");

        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(() => cache.Add(value));

        Assert.Same(failure, actual);
        Assert.Same(value, await index.Get(value.Id));
        Assert.Equal(1, cache.Statistics.Count);
        Task<CacheValue> visible = Assert.Single(cache.GetAll());
        Assert.Same(value, await visible);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-OBSERVER", "remove-fanout-and-disposal-complete")]
    public async Task ThrowingObserverBeforeIndex_RemoveUpdatesTheIndexAndDisposesTheValue()
    {
        var cache = new GreenCache<DisposableCacheValue>();
        var failure = new InvalidOperationException("Observer failure.");
        using ConnectHandle throwingConnection = cache.Connect(
            new SelectiveThrowingObserver<DisposableCacheValue>(removedFailure: failure));
        IIndex<string, DisposableCacheValue> index = cache.AddIndex("id", value => value.Id);
        var removalObserver = new RemovalObserver<DisposableCacheValue>();
        using ConnectHandle removalConnection = cache.Connect(removalObserver);
        var value = new DisposableCacheValue("key-0");
        cache.Add(value);

        Assert.True(index.Remove(value.Id));
        await removalObserver.Removed.WaitAsync(OperationTimeout, TestCancellationToken);
        await value.Disposed.WaitAsync(OperationTimeout, TestCancellationToken);

        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await index.Get(value.Id));
        Assert.Equal(0, cache.Statistics.Count);
        Assert.Empty(cache.GetAll());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-OBSERVER", "clear-fanout-completes-before-rethrow")]
    public async Task ThrowingObserverBeforeIndex_ClearRebuildsTheIndexBeforeRethrowingTheExactFailure()
    {
        var cache = new GreenCache<CacheValue>();
        var failure = new InvalidOperationException("Observer failure.");
        using ConnectHandle throwingConnection = cache.Connect(
            new SelectiveThrowingObserver<CacheValue>(clearedFailure: failure));
        IIndex<string, CacheValue> index = cache.AddIndex("id", value => value.Id);
        var value = new CacheValue("key-0");
        cache.Add(value);

        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(cache.Clear);

        Assert.Same(failure, actual);
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await index.Get(value.Id));
        Assert.Equal(0, cache.Statistics.Count);
        Assert.Empty(cache.GetAll());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-CACHE-DISPOSAL", "clear-and-rollover-are-asynchronous")]
    public async Task ClearAndAutomaticRollover_ReturnBeforeSyncOrAsyncDisposalCompletes(
        bool automaticRollover,
        bool asynchronousDisposable)
    {
        if (asynchronousDisposable)
        {
            await AssertResetDisposal(
                automaticRollover,
                probe => new BlockingAsyncDisposableValue(probe));
        }
        else
        {
            await AssertResetDisposal(
                automaticRollover,
                probe => new BlockingDisposableValue(probe));
        }
    }

    private static async Task AssertResetDisposal<TValue>(
        bool automaticRollover,
        Func<DisposalProbe, TValue> valueFactory)
        where TValue : class
    {
        var settings = new TestCacheSettings(
            capacity: 10,
            minAge: TimeSpan.FromMinutes(1),
            maxAge: TimeSpan.FromMinutes(5))
        {
            CurrentTime = DateTime.UnixEpoch,
        };
        var cache = new GreenCache<TValue>(settings);
        var probe = new DisposalProbe();
        TValue value = valueFactory(probe);
        cache.Add(value);

        Task reset = Task.Run(() =>
        {
            if (automaticRollover)
            {
                settings.CurrentTime += TimeSpan.FromHours(24);
                cache.Add(valueFactory(new DisposalProbe(completed: true)));
            }
            else
                cache.Clear();
        });

        await probe.Started.WaitAsync(OperationTimeout, TestCancellationToken);
        await reset.WaitAsync(OperationTimeout, TestCancellationToken);
        Assert.Equal(0, probe.Count);

        probe.Release();
        await probe.Completed.WaitAsync(OperationTimeout, TestCancellationToken);
        Assert.Equal(1, probe.Count);
    }

    private static TimeSpan OperationTimeout => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private sealed record CacheValue(string Id);

    private sealed class DisposableCacheValue(string id) : IDisposable
    {
        private readonly TaskCompletionSource _disposed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Disposed => _disposed.Task;

        public string Id { get; } = id;

        public void Dispose() => _disposed.TrySetResult();
    }

    private sealed class BlockingDisposableValue(DisposalProbe probe) : IDisposable
    {
        public void Dispose() => probe.DisposeSynchronously();
    }

    private sealed class BlockingAsyncDisposableValue(DisposalProbe probe) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => probe.DisposeAsynchronously();
    }

    private sealed class DisposalProbe(bool completed = false)
    {
        private readonly TaskCompletionSource _completed = NewCompletionSource(completed);
        private readonly TaskCompletionSource _release = NewCompletionSource(completed);
        private readonly TaskCompletionSource _started = NewCompletionSource(completed);
        private int _count;

        public Task Started => _started.Task;

        public Task Completed => _completed.Task;

        public int Count => Volatile.Read(ref _count);

        public void Release() => _release.TrySetResult();

        public void DisposeSynchronously()
        {
            _started.TrySetResult();
            _release.Task.WaitAsync(OperationTimeout, TestCancellationToken).GetAwaiter().GetResult();
            Interlocked.Increment(ref _count);
            _completed.TrySetResult();
        }

        public async ValueTask DisposeAsynchronously()
        {
            _started.TrySetResult();
            await _release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            Interlocked.Increment(ref _count);
            _completed.TrySetResult();
        }

        private static TaskCompletionSource NewCompletionSource(bool completed)
        {
            var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            if (completed)
                source.SetResult();

            return source;
        }
    }

    private sealed class SelectiveThrowingObserver<TValue>(
        Exception? addedFailure = null,
        Exception? removedFailure = null,
        Exception? clearedFailure = null) : ICacheValueObserver<TValue>
        where TValue : class
    {
        public void ValueAdded(INode<TValue> node, TValue value)
        {
            if (addedFailure is not null)
                throw addedFailure;
        }

        public void ValueRemoved(INode<TValue> node, TValue value)
        {
            if (removedFailure is not null)
                throw removedFailure;
        }

        public void CacheCleared()
        {
            if (clearedFailure is not null)
                throw clearedFailure;
        }
    }

    private sealed class RemovalObserver<TValue> : ICacheValueObserver<TValue>
        where TValue : class
    {
        private readonly TaskCompletionSource _removed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Removed => _removed.Task;

        public void ValueAdded(INode<TValue> node, TValue value)
        {
        }

        public void ValueRemoved(INode<TValue> node, TValue value) => _removed.TrySetResult();

        public void CacheCleared()
        {
        }
    }
}
