using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Caching;

public sealed class ResourceCacheExpiredDuplicateOwnershipTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-CACHE-EXPIRATION", "expired-ownership-release-before-duplicate-add-outcome")]
    public async Task AddAsync_ExpiredOwnershipIsReleasedEvenWhenTheNewKeyIsDuplicateAsync(bool duplicate)
    {
        var time = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var expired = new OwnedResource("expired");
        var live = new OwnedResource("live");
        var candidate = new OwnedResource(duplicate ? "live" : "new");
        var cache = new ResourceCache<OwnedResource>(new ResourceCacheOptions(
            capacity: 4,
            maxAge: TimeSpan.FromSeconds(9),
            expirationMode: ResourceCacheExpirationMode.Absolute,
            timeProvider: time,
            cleanupInterval: TimeSpan.FromDays(1)));
        var operations = new List<Task>();
        Task? disposal = null;
        Task? expectedFaultTask = null;
        Exception? expectedFault = null;

        try
        {
            IResourceCacheIndex<string, OwnedResource> index = cache.AddIndex("id", value => value.Id);

            Task firstAdd = cache.AddAsync(expired, CancellationToken.None).AsTask();
            operations.Add(firstAdd);
            await firstAdd.WaitAsync(OperationTimeout, CancellationToken.None);
            time.Advance(TimeSpan.FromSeconds(5));

            Task secondAdd = cache.AddAsync(live, CancellationToken.None).AsTask();
            operations.Add(secondAdd);
            await secondAdd.WaitAsync(OperationTimeout, CancellationToken.None);
            Assert.Equal(2, cache.Statistics.Count);
            Assert.Equal(0, expired.DisposeCalls);
            Assert.Equal(0, live.DisposeCalls);
            time.Advance(TimeSpan.FromSeconds(5));

            Task add = cache.AddAsync(candidate, CancellationToken.None).AsTask();
            operations.Add(add);
            Exception? failure = await Record.ExceptionAsync(async () =>
            {
                await add.WaitAsync(OperationTimeout, CancellationToken.None);
            });

            if (duplicate)
            {
                InvalidOperationException duplicateFailure = Assert.IsType<InvalidOperationException>(failure);
                Assert.Contains("already contains key", duplicateFailure.Message, StringComparison.Ordinal);
                Assert.Contains("live", duplicateFailure.Message, StringComparison.Ordinal);
                Assert.True(add.IsFaulted);
                AggregateException? aggregate = add.Exception;
                Assert.NotNull(aggregate);
                Assert.Same(duplicateFailure, Assert.Single(aggregate.InnerExceptions));
                expectedFaultTask = add;
                expectedFault = duplicateFailure;
            }
            else
            {
                Assert.Null(failure);
                Assert.True(add.IsCompletedSuccessfully);
            }

            Assert.Equal(duplicate ? 1 : 2, cache.Statistics.Count);
            Assert.Equal(1L, cache.Statistics.Evictions);
            Assert.DoesNotContain(cache.GetValues(CancellationToken.None), value => ReferenceEquals(value, expired));
            Assert.Equal(0, live.DisposeCalls);
            Assert.Equal(0, candidate.DisposeCalls);

            // Both public outcomes have removed the expired cache-owned resource before this assertion.
            Assert.Equal(1, expired.DisposeCalls);

            Task<OwnedResource> lookup = index.GetAsync("live", CancellationToken.None).AsTask();
            operations.Add(lookup);
            Assert.Same(live, await lookup.WaitAsync(OperationTimeout, CancellationToken.None));
            if (duplicate)
                Assert.DoesNotContain(cache.GetValues(CancellationToken.None), value => ReferenceEquals(value, candidate));
            else
                Assert.Contains(cache.GetValues(CancellationToken.None), value => ReferenceEquals(value, candidate));

            disposal = cache.DisposeAsync().AsTask();
            await disposal.WaitAsync(OperationTimeout, CancellationToken.None);
            Assert.Equal(1, expired.DisposeCalls);
            Assert.Equal(1, live.DisposeCalls);
            Assert.Equal(duplicate ? 0 : 1, candidate.DisposeCalls);
        }
        finally
        {
            try
            {
                await ObserveEveryStartedTaskAsync(operations, 0, expectedFaultTask, expectedFault);
            }
            finally
            {
                disposal ??= cache.DisposeAsync().AsTask();
                try
                {
                    await ObserveTerminalAsync(disposal);
                }
                finally
                {
                    // A fallback is permitted only after the real cache owner has reached a terminal outcome.
                    if (disposal.IsCompleted)
                        await DisposeUnreleasedResourcesAsync([expired, live, candidate], 0);
                }
            }
        }
    }

    private static TimeSpan OperationTimeout => TimeSpan.FromSeconds(10);

    private static async Task ObserveEveryStartedTaskAsync(IReadOnlyList<Task> tasks, int index,
        Task? expectedFaultTask, Exception? expectedFault)
    {
        if (index == tasks.Count)
            return;

        try
        {
            await ObserveTerminalAsync(tasks[index], expectedFaultTask, expectedFault);
        }
        finally
        {
            await ObserveEveryStartedTaskAsync(tasks, index + 1, expectedFaultTask, expectedFault);
        }
    }

    private static async Task ObserveTerminalAsync(Task task, Task? expectedFaultTask = null, Exception? expectedFault = null)
    {
        try
        {
            await task.WaitAsync(OperationTimeout, CancellationToken.None);
        }
        catch (Exception exception) when (task.IsCompleted && exception is not TimeoutException
            && ReferenceEquals(task, expectedFaultTask) && ReferenceEquals(exception, expectedFault))
        {
            // Only the exact validated duplicate-add cause may be observed; unknown faults and timeouts remain visible.
        }
    }

    private static async Task DisposeUnreleasedResourcesAsync(IReadOnlyList<OwnedResource> resources, int index)
    {
        if (index == resources.Count)
            return;

        try
        {
            OwnedResource resource = resources[index];
            if (resource.DisposeCalls == 0)
                await resource.DisposeAsync().AsTask().WaitAsync(OperationTimeout, CancellationToken.None);
        }
        finally
        {
            await DisposeUnreleasedResourcesAsync(resources, index + 1);
        }
    }

    private sealed class OwnedResource(string id) : IAsyncDisposable
    {
        private int _disposeCalls;

        public string Id { get; } = id;
        public int DisposeCalls => Volatile.Read(ref _disposeCalls);

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCalls);
            return ValueTask.CompletedTask;
        }
    }
}
