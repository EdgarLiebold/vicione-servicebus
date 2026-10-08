using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Caching;

public sealed class ResourceCacheComparerIsolationTests(ITestOutputHelper output)
{
    static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);

    [Theory, InlineData(false), InlineData(true)]
    [RequirementCoverage("REQ-VSB-CACHE-MULTI-INDEX", "secondary-comparer-does-not-break-committed-resource-retirement")]
    public async Task RemoveRetiresEveryIndexAndReleasesResource(bool hostile)
    {
        await using var fixture = new Fixture();
        var comparer = new Comparer();
        var primary = fixture.Cache.AddIndex("primary", x => x.Id);
        var secondary = fixture.Cache.AddIndex("secondary", x => x.Id, comparer: comparer);
        var resource = fixture.New("old");
        await fixture.RunAsync(fixture.Cache.AddAsync(resource, TestContext.Current.CancellationToken).AsTask());
        comparer.ArmedKey = hostile ? "old" : null;
        bool removed = false;
        Exception? failure = await Record.ExceptionAsync(async () =>
            removed = await fixture.RunAsync(primary.RemoveAsync("old", TestContext.Current.CancellationToken).AsTask()));
        output.WriteLine("ACTUAL_REMOVE_FAILURE: " + failure);
        comparer.ArmedKey = null;
        (Owned? first, Exception? firstFailure) = await fixture.ReadAsync(primary, "old");
        (Owned? second, Exception? secondFailure) = await fixture.ReadAsync(secondary, "old");
        output.WriteLine($"STATE release={resource.DisposeCalls} count={fixture.Cache.Statistics.Count} primary={first?.Id} secondary={second?.Id}");
        Assert.Equal(1, resource.DisposeCalls);
        Assert.Equal(0, fixture.Cache.Statistics.Count);
        Assert.Equal(0, fixture.Cache.Statistics.PendingCreations);
        Assert.Empty(fixture.Cache.GetValues(TestContext.Current.CancellationToken));
        Assert.Null(first);
        Assert.IsType<KeyNotFoundException>(firstFailure);
        Assert.Null(second);
        Assert.IsType<KeyNotFoundException>(secondFailure);
        Assert.Equal(1, fixture.Observer.Removed);
        Assert.Null(failure);
        Assert.True(removed);
        Assert.Equal(0, comparer.Throws);
        var replacement = fixture.New("replacement");
        await fixture.RunAsync(fixture.Cache.AddAsync(replacement, TestContext.Current.CancellationToken).AsTask());
        Assert.Same(replacement, await fixture.RunAsync(primary.GetAsync("replacement", TestContext.Current.CancellationToken).AsTask()));
        await fixture.StopAsync();
        Assert.Equal(1, resource.DisposeCalls);
        Assert.Equal(1, replacement.DisposeCalls);
    }

    [Theory, InlineData(false), InlineData(true)]
    [RequirementCoverage("REQ-VSB-CACHE-DIRECT-ADD", "direct-admission-comparer-failure-retains-caller-ownership-and-no-index-ghost")]
    public async Task DirectAdmissionPublishesAllIndicesTogether(bool hostile)
    {
        await using var fixture = new Fixture();
        var comparer = new Comparer { ArmedKey = hostile ? "candidate" : null };
        var primary = fixture.Cache.AddIndex("primary", x => x.Id);
        var secondary = fixture.Cache.AddIndex("secondary", x => x.Id, comparer: comparer);
        var candidate = fixture.New("candidate");
        Exception? failure = await Record.ExceptionAsync(() => fixture.RunAsync(fixture.Cache.AddAsync(candidate, TestContext.Current.CancellationToken).AsTask()));
        output.WriteLine("ACTUAL_ADMISSION_FAILURE: " + failure);
        comparer.ArmedKey = null;
        (Owned? first, Exception? firstFailure) = await fixture.ReadAsync(primary, candidate.Id);
        (Owned? second, Exception? secondFailure) = await fixture.ReadAsync(secondary, candidate.Id);
        Assert.Equal(0, candidate.DisposeCalls);
        Assert.Equal(hostile ? 0 : 1, fixture.Cache.Statistics.Count);
        Assert.Equal(0, fixture.Cache.Statistics.PendingCreations);
        Assert.Equal(hostile ? 0 : 1, fixture.Cache.Statistics.TotalCreated);
        Assert.Equal(hostile ? 0 : 1, fixture.Cache.GetValues(TestContext.Current.CancellationToken).Count);
        if (!hostile) Assert.Same(candidate, Assert.Single(fixture.Cache.GetValues(TestContext.Current.CancellationToken)));
        if (hostile)
        {
            Assert.Null(first);
            Assert.IsType<KeyNotFoundException>(firstFailure);
            Assert.Null(second);
            Assert.IsType<KeyNotFoundException>(secondFailure);
            Assert.Same(comparer.Failure, failure);
            Assert.Equal(1, comparer.Throws);
        }
        else
        {
            Assert.Same(candidate, first);
            Assert.Same(candidate, second);
            Assert.Null(firstFailure);
            Assert.Null(secondFailure);
            Assert.Null(failure);
        }
        Assert.Equal(hostile ? 0 : 1, fixture.Observer.Added);
        await fixture.StopAsync();
        Assert.Equal(hostile ? 0 : 1, candidate.DisposeCalls);
    }

    [Theory, InlineData(false), InlineData(true)]
    [RequirementCoverage("REQ-VSB-CACHE-INDEX-FACTORY", "factory-admission-comparer-failure-releases-rejected-value-and-removes-all-index-ghosts")]
    public async Task FactoryAdmissionPublishesAllIndicesTogether(bool hostile)
    {
        await using var fixture = new Fixture();
        var comparer = new Comparer { ArmedKey = hostile ? "candidate" : null };
        var primary = fixture.Cache.AddIndex("primary", x => x.Id);
        var secondary = fixture.Cache.AddIndex("secondary", x => x.Id, comparer: comparer);
        var candidate = fixture.New("candidate");
        int calls = 0;
        Owned? actual = null;
        Exception? failure = await Record.ExceptionAsync(async () => actual = await fixture.RunAsync(primary.GetOrAddAsync(candidate.Id,
            (_, _) => { calls++; return ValueTask.FromResult(candidate); }, TestContext.Current.CancellationToken).AsTask()));
        output.WriteLine("ACTUAL_FACTORY_FAILURE: " + failure);
        comparer.ArmedKey = null;
        (Owned? first, Exception? firstFailure) = await fixture.ReadAsync(primary, candidate.Id);
        (Owned? second, Exception? secondFailure) = await fixture.ReadAsync(secondary, candidate.Id);
        Assert.Equal(1, calls);
        Assert.Equal(hostile ? 1 : 0, candidate.DisposeCalls);
        Assert.Equal(hostile ? 0 : 1, fixture.Cache.Statistics.Count);
        Assert.Equal(0, fixture.Cache.Statistics.PendingCreations);
        Assert.Equal(hostile ? 0 : 1, fixture.Cache.Statistics.TotalCreated);
        Assert.Equal(hostile ? 1 : 0, fixture.Cache.Statistics.CreationFaults);
        if (hostile) Assert.Empty(fixture.Cache.GetValues(TestContext.Current.CancellationToken));
        else Assert.Same(candidate, Assert.Single(fixture.Cache.GetValues(TestContext.Current.CancellationToken)));
        if (hostile)
        {
            Assert.Null(first);
            Assert.IsType<KeyNotFoundException>(firstFailure);
            Assert.Null(second);
            Assert.IsType<KeyNotFoundException>(secondFailure);
            Assert.Null(actual);
            Assert.Same(comparer.Failure, failure);
        }
        else
        {
            Assert.Same(candidate, first);
            Assert.Same(candidate, second);
            Assert.Same(candidate, actual);
            Assert.Null(firstFailure);
            Assert.Null(secondFailure);
            Assert.Null(failure);
        }
        Assert.Equal(hostile ? 0 : 1, fixture.Observer.Added);
        if (hostile)
        {
            var replacement = fixture.New("replacement");
            Assert.Same(replacement, await fixture.RunAsync(primary.GetOrAddAsync(replacement.Id,
                (_, _) => ValueTask.FromResult(replacement), TestContext.Current.CancellationToken).AsTask()));
        }
        await fixture.StopAsync();
        Assert.Equal(1, candidate.DisposeCalls);
    }

    [Theory, InlineData(false), InlineData(true)]
    [RequirementCoverage("REQ-VSB-CACHE-EXPIRATION", "lookup-failure-after-expiration-still-releases-retired-resource")]
    public async Task LookupFailureCannotStrandAlreadyExpiredOwnership(bool hostile)
    {
        await using var fixture = new Fixture();
        var comparer = new Comparer();
        var index = fixture.Cache.AddIndex("primary", x => x.Id, comparer: comparer);
        var expired = fixture.New("expired");
        await fixture.RunAsync(fixture.Cache.AddAsync(expired, TestContext.Current.CancellationToken).AsTask());
        fixture.Time.Advance(TimeSpan.FromSeconds(9) + TimeSpan.FromTicks(1));
        comparer.ArmedKey = hostile ? "missing" : null;
        (_, Exception? failure) = await fixture.ReadAsync(index, "missing");
        output.WriteLine("ACTUAL_LOOKUP_FAILURE: " + failure);
        comparer.ArmedKey = null;
        Assert.Equal(1, expired.DisposeCalls);
        Assert.Equal(0, fixture.Cache.Statistics.Count);
        Assert.Equal(0, fixture.Cache.Statistics.PendingCreations);
        Assert.Equal(1, fixture.Cache.Statistics.Evictions);
        Assert.Equal(1, fixture.Observer.Removed);
        Assert.Empty(fixture.Cache.GetValues(TestContext.Current.CancellationToken));
        if (hostile) Assert.Same(comparer.Failure, failure);
        else Assert.IsType<KeyNotFoundException>(failure);
        var replacement = fixture.New("replacement");
        await fixture.RunAsync(fixture.Cache.AddAsync(replacement, TestContext.Current.CancellationToken).AsTask());
        Assert.Same(replacement, await fixture.RunAsync(index.GetAsync(replacement.Id, TestContext.Current.CancellationToken).AsTask()));
        await fixture.StopAsync();
        Assert.Equal(1, expired.DisposeCalls);
        Assert.Equal(1, replacement.DisposeCalls);
    }

    [Theory, InlineData(false), InlineData(true)]
    [RequirementCoverage("REQ-VSB-CACHE-MULTI-INDEX", "colliding-equality-preparation-failure-leaves-anchor-and-caller-ownership-intact")]
    public async Task CollisionEqualityFailureLeavesExistingOwnershipIntact(bool hostile)
    {
        await using var fixture = new Fixture(capacity: 2);
        var comparer = new Comparer { Collisions = true };
        var primary = fixture.Cache.AddIndex("primary", x => x.Id);
        var secondary = fixture.Cache.AddIndex("secondary", x => x.Id, comparer: comparer);
        var anchor = fixture.New("anchor");
        var candidate = fixture.New("candidate");
        await fixture.RunAsync(fixture.Cache.AddAsync(anchor, TestContext.Current.CancellationToken).AsTask());
        comparer.ThrowEquality = hostile;
        Exception? failure = await Record.ExceptionAsync(() => fixture.RunAsync(fixture.Cache.AddAsync(candidate, TestContext.Current.CancellationToken).AsTask()));
        output.WriteLine("ACTUAL_EQUALITY_FAILURE: " + failure);
        comparer.ThrowEquality = false;
        Assert.Equal(0, anchor.DisposeCalls);
        Assert.Equal(0, candidate.DisposeCalls);
        Assert.Same(anchor, await fixture.RunAsync(primary.GetAsync(anchor.Id, TestContext.Current.CancellationToken).AsTask()));
        Assert.Same(anchor, await fixture.RunAsync(secondary.GetAsync(anchor.Id, TestContext.Current.CancellationToken).AsTask()));
        Assert.Equal(hostile ? 1 : 2, fixture.Cache.Statistics.Count);
        Assert.Equal(hostile ? 1 : 2, fixture.Cache.Statistics.TotalCreated);
        Assert.Equal(hostile ? 1 : 2, fixture.Observer.Added);
        if (hostile)
        {
            (_, Exception? absent) = await fixture.ReadAsync(primary, candidate.Id);
            Assert.IsType<KeyNotFoundException>(absent);
            (_, Exception? secondaryAbsent) = await fixture.ReadAsync(secondary, candidate.Id);
            Assert.IsType<KeyNotFoundException>(secondaryAbsent);
            Assert.Same(anchor, Assert.Single(fixture.Cache.GetValues(TestContext.Current.CancellationToken)));
            Assert.Same(comparer.Failure, failure);
        }
        else
        {
            Assert.Null(failure);
            Assert.Same(candidate, await fixture.RunAsync(secondary.GetAsync(candidate.Id, TestContext.Current.CancellationToken).AsTask()));
            Assert.Same(candidate, await fixture.RunAsync(primary.GetAsync(candidate.Id, TestContext.Current.CancellationToken).AsTask()));
            Assert.Equal(2, fixture.Cache.GetValues(TestContext.Current.CancellationToken).Count);
            Assert.Contains(fixture.Cache.GetValues(TestContext.Current.CancellationToken), x => ReferenceEquals(x, anchor));
            Assert.Contains(fixture.Cache.GetValues(TestContext.Current.CancellationToken), x => ReferenceEquals(x, candidate));
        }
        await fixture.StopAsync();
        Assert.Equal(1, anchor.DisposeCalls);
        Assert.Equal(hostile ? 0 : 1, candidate.DisposeCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CACHE-INDEX-FACTORY", "late-invalidated-factory-cleanup-cannot-remove-a-still-pending-replacement-slot")]
    public async Task LateOldFactoryCannotUnlinkPendingReplacement()
    {
        await using var fixture = new Fixture(capacity: 2);
        var index = fixture.Cache.AddIndex("primary", x => x.Id);
        var old = fixture.New("shared");
        var replacement = fixture.New("shared");
        var oldStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var newStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var oldRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var newRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int oldCalls = 0;
        int newCalls = 0;
        int unwantedCalls = 0;
        Task<Owned>? oldRequest = null;
        Task? clear = null;
        Task<Owned>? newRequest = null;
        Task<Owned>? lookup = null;
        Task<Owned>? waiter = null;
        Task<Owned>? laterLookup = null;
        Task<Owned>? laterWaiter = null;
        try
        {
            oldRequest = fixture.Own(index.GetOrAddAsync("shared", async (_, _) =>
            {
                oldCalls++;
                oldStarted.TrySetResult();
                await oldRelease.Task.ConfigureAwait(false); // Owned fixture gate; released unconditionally below.
                return old;
            }, TestContext.Current.CancellationToken).AsTask());
            await oldStarted.Task.WaitAsync(Bound, TestContext.Current.CancellationToken);
            clear = fixture.Own(fixture.Cache.ClearAsync(TestContext.Current.CancellationToken).AsTask());
            Exception? invalidated = await Record.ExceptionAsync(() => fixture.RunAsync(oldRequest));
            Assert.IsType<OperationCanceledException>(invalidated);
            Assert.False(clear.IsCompleted);
            Assert.Equal(1, fixture.Cache.Statistics.PendingCreations);
            newRequest = fixture.Own(index.GetOrAddAsync("shared", async (_, _) =>
            {
                newCalls++;
                newStarted.TrySetResult();
                await newRelease.Task.ConfigureAwait(false);
                return replacement;
            }, TestContext.Current.CancellationToken).AsTask());
            await newStarted.Task.WaitAsync(Bound, TestContext.Current.CancellationToken);
            lookup = fixture.Own(index.GetAsync("shared", TestContext.Current.CancellationToken).AsTask());
            waiter = fixture.Own(index.GetOrAddAsync("shared", (_, _) =>
            {
                unwantedCalls++;
                return ValueTask.FromResult(fixture.New("unwanted"));
            }, TestContext.Current.CancellationToken).AsTask());
            Assert.Equal(2, fixture.Cache.Statistics.PendingCreations);
            Assert.False(newRequest.IsCompleted);
            Assert.False(lookup.IsCompleted);
            Assert.False(waiter.IsCompleted);
            oldRelease.TrySetResult();
            await fixture.RunAsync(clear);
            laterLookup = fixture.Own(index.GetAsync("shared", TestContext.Current.CancellationToken).AsTask());
            laterWaiter = fixture.Own(index.GetOrAddAsync("shared", (_, _) =>
            {
                unwantedCalls++;
                return ValueTask.FromResult(fixture.New("unwanted-later"));
            }, TestContext.Current.CancellationToken).AsTask());
            // Fresh reads after old cleanup test the live pending map, rather than merely
            // observing waiters that already captured the replacement Completion task.
            if (laterLookup.IsCompleted)
                output.WriteLine("ACTUAL_LATE_LOOKUP: " + await Record.ExceptionAsync(() => fixture.RunAsync(laterLookup)));
            if (laterWaiter.IsCompleted)
                output.WriteLine("ACTUAL_LATE_WAITER: " + await Record.ExceptionAsync(() => fixture.RunAsync(laterWaiter)));
            Assert.Equal(1, old.DisposeCalls);
            Assert.Equal(0, replacement.DisposeCalls);
            Assert.Equal(1, fixture.Cache.Statistics.PendingCreations);
            Assert.Equal(0, fixture.Cache.Statistics.Count);
            Assert.Equal(1, oldCalls);
            Assert.Equal(1, newCalls);
            Assert.Equal(0, unwantedCalls);
            Assert.False(newRequest.IsCompleted);
            Assert.False(lookup.IsCompleted);
            Assert.False(waiter.IsCompleted);
            Assert.False(laterLookup.IsCompleted);
            Assert.False(laterWaiter.IsCompleted);
            newRelease.TrySetResult();
            Assert.Same(replacement, await fixture.RunAsync(newRequest));
            Assert.Same(replacement, await fixture.RunAsync(lookup));
            Assert.Same(replacement, await fixture.RunAsync(waiter));
            Assert.Same(replacement, await fixture.RunAsync(laterLookup));
            Assert.Same(replacement, await fixture.RunAsync(laterWaiter));
            Assert.Equal(0, unwantedCalls);
            Assert.Equal(0, fixture.Cache.Statistics.PendingCreations);
            Assert.Same(replacement, Assert.Single(fixture.Cache.GetValues(TestContext.Current.CancellationToken)));
            await fixture.StopAsync();
            Assert.Equal(1, old.DisposeCalls);
            Assert.Equal(1, replacement.DisposeCalls);
        }
        finally
        {
            oldRelease.TrySetResult();
            newRelease.TrySetResult();
            // The fixture then observes every actual owned request/clear/lookup/waiter and
            // joins cache terminal cleanup before any fallback resource rescue.
        }
    }

    [Theory, InlineData(false), InlineData(true)]
    [RequirementCoverage("REQ-VSB-CACHE-MULTI-INDEX", "failed-bulk-index-cannot-attach-unpublished-retirement-metadata")]
    public async Task FailedBulkIndexCannotCorruptExistingRetirement(bool hostile)
    {
        await using var fixture = new Fixture(capacity: 2);
        var primary = fixture.Cache.AddIndex("primary", x => x.Id);
        var first = fixture.New("first");
        var second = fixture.New("second");
        await fixture.RunAsync(fixture.Cache.AddAsync(first, TestContext.Current.CancellationToken).AsTask());
        await fixture.RunAsync(fixture.Cache.AddAsync(second, TestContext.Current.CancellationToken).AsTask());
        var comparer = new BulkComparer(output) { FailSecondHash = hostile };
        IResourceCacheIndex<string, Owned>? added = null;
        Exception? additionFailure = Record.Exception(() =>
            added = fixture.Cache.AddIndex("bulk", x => x.Id, comparer: comparer));
        output.WriteLine("ACTUAL_BULK_ADDITION_FAILURE: " + additionFailure);
        comparer.FailSecondHash = false;
        Assert.Equal(0, first.DisposeCalls);
        Assert.Equal(0, second.DisposeCalls);
        Assert.Equal(2, fixture.Cache.Statistics.Count);
        Assert.Equal(2, fixture.Cache.Statistics.TotalCreated);
        Assert.Equal(0, fixture.Cache.Statistics.PendingCreations);
        Assert.Equal(2, fixture.Observer.Added);
        Assert.Same(first, await fixture.RunAsync(primary.GetAsync(first.Id, TestContext.Current.CancellationToken).AsTask()));
        Assert.Same(second, await fixture.RunAsync(primary.GetAsync(second.Id, TestContext.Current.CancellationToken).AsTask()));
        var snapshotBeforeRemoval = fixture.Cache.GetValues(TestContext.Current.CancellationToken);
        Assert.Equal(2, snapshotBeforeRemoval.Count);
        Assert.Contains(snapshotBeforeRemoval, value => ReferenceEquals(first, value));
        Assert.Contains(snapshotBeforeRemoval, value => ReferenceEquals(second, value));
        if (hostile)
        {
            Assert.Null(added);
            Assert.Same(comparer.Failure, additionFailure);
            Assert.Equal(2, comparer.SecondCalls);
            Assert.Throws<KeyNotFoundException>(() => fixture.Cache.GetIndex<string>("bulk"));
        }
        else
        {
            Assert.Null(additionFailure);
            Assert.Same(added, fixture.Cache.GetIndex<string>("bulk"));
            Assert.Same(first, await fixture.RunAsync(added!.GetAsync(first.Id, TestContext.Current.CancellationToken).AsTask()));
            Assert.Same(second, await fixture.RunAsync(added!.GetAsync(second.Id, TestContext.Current.CancellationToken).AsTask()));
        }

        bool removed = false;
        Exception? removalFailure;
        comparer.ThrowFirst = hostile;
        try
        {
            removalFailure = await Record.ExceptionAsync(async () =>
                removed = await fixture.RunAsync(primary.RemoveAsync(first.Id, TestContext.Current.CancellationToken).AsTask()));
            output.WriteLine("ACTUAL_BULK_RETIREMENT_FAILURE: " + removalFailure);
        }
        finally { comparer.ThrowFirst = false; }
        // Physical ownership is checked before reference/error oracles or fixture rescue.
        Assert.Equal(1, first.DisposeCalls);
        Assert.Equal(0, second.DisposeCalls);
        Assert.Equal(1, fixture.Cache.Statistics.Count);
        Assert.Equal(1, fixture.Observer.Removed);
        Assert.Equal(hostile ? 1 : 0, comparer.Throws);
        Assert.Null(removalFailure);
        Assert.True(removed);
        (_, Exception? missing) = await fixture.ReadAsync(primary, first.Id);
        Assert.IsType<KeyNotFoundException>(missing);
        Assert.Same(second, await fixture.RunAsync(primary.GetAsync(second.Id, TestContext.Current.CancellationToken).AsTask()));
        Assert.Same(second, Assert.Single(fixture.Cache.GetValues(TestContext.Current.CancellationToken)));
        if (hostile)
            added = fixture.Cache.AddIndex("bulk", x => x.Id, comparer: comparer);
        Assert.Same(second, await fixture.RunAsync(added!.GetAsync(second.Id, TestContext.Current.CancellationToken).AsTask()));
        Assert.True(await fixture.RunAsync(primary.RemoveAsync(second.Id, TestContext.Current.CancellationToken).AsTask()));
        Assert.Equal(1, second.DisposeCalls);
        Assert.Equal(0, fixture.Cache.Statistics.Count);
        Assert.Equal(2, fixture.Observer.Removed);
        var replacement = fixture.New("replacement");
        await fixture.RunAsync(fixture.Cache.AddAsync(replacement, TestContext.Current.CancellationToken).AsTask());
        Assert.Same(replacement, await fixture.RunAsync(added!.GetAsync(replacement.Id, TestContext.Current.CancellationToken).AsTask()));
        await fixture.StopAsync();
        Assert.Equal(1, first.DisposeCalls);
        Assert.Equal(1, second.DisposeCalls);
        Assert.Equal(1, replacement.DisposeCalls);
    }

    [Theory]
    [InlineData("get", false), InlineData("get", true)]
    [InlineData("get-or-add", false), InlineData("get-or-add", true)]
    [InlineData("remove", false), InlineData("remove", true)]
    [InlineData("add", false), InlineData("add", true)]
    [InlineData("cleanup", false), InlineData("cleanup", true)]
    [InlineData("timer", false), InlineData("timer", true)]
    [RequirementCoverage("REQ-VSB-CACHE-EXPIRATION", "partial-expiration-clock-fault-releases-retired-owners-across-public-and-timed-cleanup")]
    public async Task PartialExpirationClockFailureCannotLoseRetiredOwnership(string operation, bool hostile)
    {
        var clock = new ScanClock();
        await using var fixture = new Fixture(capacity: 2, timeProvider: clock);
        var index = fixture.Cache.AddIndex("primary", x => x.Id);
        var first = fixture.New("first");
        var second = fixture.New("second");
        var candidate = fixture.New("candidate");
        await fixture.RunAsync(fixture.Cache.AddAsync(first, TestContext.Current.CancellationToken).AsTask());
        await fixture.RunAsync(fixture.Cache.AddAsync(second, TestContext.Current.CancellationToken).AsTask());
        clock.Time.Advance(TimeSpan.FromSeconds(9) + TimeSpan.FromTicks(1));
        clock.Arm(hostile);
        int factoryCalls = 0;
        Exception? failure;
        try
        {
            failure = operation == "timer" ? Record.Exception(clock.Fire) : await Record.ExceptionAsync(async () =>
            {
                switch (operation)
                {
                    case "get": await fixture.RunAsync(index.GetAsync("missing", TestContext.Current.CancellationToken).AsTask()); break;
                    case "get-or-add":
                        await fixture.RunAsync(index.GetOrAddAsync(candidate.Id, (_, _) =>
                        { factoryCalls++; return ValueTask.FromResult(candidate); }, TestContext.Current.CancellationToken).AsTask()); break;
                    case "remove": await fixture.RunAsync(index.RemoveAsync("missing", TestContext.Current.CancellationToken).AsTask()); break;
                    case "add": await fixture.RunAsync(fixture.Cache.AddAsync(candidate, TestContext.Current.CancellationToken).AsTask()); break;
                    case "cleanup": await fixture.RunAsync(fixture.Cache.CleanupExpiredAsync(TestContext.Current.CancellationToken).AsTask()); break;
                    default: throw new InvalidOperationException("Unknown operation: " + operation);
                }
            });
            output.WriteLine($"ACTUAL_SCAN_FAILURE operation={operation} hostile={hostile}: {failure}");
            output.WriteLine($"PHYSICAL first={first.DisposeCalls} second={second.DisposeCalls} frequencyReads={clock.Reads}");
        }
        finally { clock.Arm(false); }

        // No scan-order assumption: precisely one retired resource precedes the third required frequency read.
        Assert.Equal(hostile ? 1 : 2, first.DisposeCalls + second.DisposeCalls);
        Assert.Equal(0, candidate.DisposeCalls);
        bool candidateCommitted = !hostile && (operation is "add" or "get-or-add");
        Assert.Equal(hostile || candidateCommitted ? 1 : 0, fixture.Cache.Statistics.Count);
        Assert.Equal(0, fixture.Cache.Statistics.PendingCreations);
        Assert.Equal(candidateCommitted ? 3 : 2, fixture.Cache.Statistics.TotalCreated);
        Assert.Equal(candidateCommitted ? 3 : 2, fixture.Observer.Added);
        Assert.Equal(hostile ? 1 : 2, fixture.Observer.Removed);
        Assert.Equal(!hostile && operation == "get-or-add" ? 1 : 0, factoryCalls);
        if (hostile)
        {
            var survivor = Assert.Single(fixture.Cache.GetValues(TestContext.Current.CancellationToken));
            Assert.True(ReferenceEquals(first, survivor) || ReferenceEquals(second, survivor));
            Assert.Equal(0, survivor.DisposeCalls);
            if (operation == "timer") Assert.Null(failure);
            else Assert.Same(clock.Failure, failure);
        }
        else
        {
            if (operation == "get") Assert.IsType<KeyNotFoundException>(failure);
            else Assert.Null(failure);
            if (candidateCommitted) Assert.Same(candidate, Assert.Single(fixture.Cache.GetValues(TestContext.Current.CancellationToken)));
            else Assert.Empty(fixture.Cache.GetValues(TestContext.Current.CancellationToken));
        }
        await fixture.RunAsync(fixture.Cache.CleanupExpiredAsync(TestContext.Current.CancellationToken).AsTask());
        Assert.Equal(1, first.DisposeCalls);
        Assert.Equal(1, second.DisposeCalls);
        if (!candidateCommitted)
            await fixture.RunAsync(fixture.Cache.AddAsync(candidate, TestContext.Current.CancellationToken).AsTask());
        Assert.Same(candidate, await fixture.RunAsync(index.GetAsync(candidate.Id, TestContext.Current.CancellationToken).AsTask()));
        await fixture.StopAsync();
        Assert.Equal(1, first.DisposeCalls);
        Assert.Equal(1, second.DisposeCalls);
        Assert.Equal(1, candidate.DisposeCalls);
    }

    [Theory, InlineData(0), InlineData(1), InlineData(2)]
    [RequirementCoverage("REQ-VSB-CACHE-MULTI-INDEX", "colliding-head-middle-tail-retirement-preserves-survivors-and-key-reuse")]
    public async Task CollidingResourceRemovalPreservesEveryOtherOwnedResource(int position)
    {
        await using var fixture = new Fixture(capacity: 3);
        var primary = fixture.Cache.AddIndex("primary", x => x.Id);
        var secondary = fixture.Cache.AddIndex("secondary", x => x.Id, comparer: new Comparer { Collisions = true });
        Owned[] resources = [fixture.New("first"), fixture.New("middle"), fixture.New("last")];
        foreach (Owned resource in resources)
            await fixture.RunAsync(fixture.Cache.AddAsync(resource, TestContext.Current.CancellationToken).AsTask());
        Owned removed = resources[position];
        Assert.True(await fixture.RunAsync(primary.RemoveAsync(removed.Id, TestContext.Current.CancellationToken).AsTask()));
        Assert.Equal(1, removed.DisposeCalls);
        Assert.Equal(2, fixture.Cache.Statistics.Count);
        Assert.Equal(1, fixture.Observer.Removed);
        foreach (Owned resource in resources.Where(resource => !ReferenceEquals(resource, removed)))
        {
            Assert.Equal(0, resource.DisposeCalls);
            Assert.Same(resource, await fixture.RunAsync(primary.GetAsync(resource.Id, TestContext.Current.CancellationToken).AsTask()));
            Assert.Same(resource, await fixture.RunAsync(secondary.GetAsync(resource.Id, TestContext.Current.CancellationToken).AsTask()));
        }
        (_, Exception? missing) = await fixture.ReadAsync(secondary, removed.Id);
        Assert.IsType<KeyNotFoundException>(missing);
        var replacement = fixture.New(removed.Id);
        await fixture.RunAsync(fixture.Cache.AddAsync(replacement, TestContext.Current.CancellationToken).AsTask());
        Assert.Same(replacement, await fixture.RunAsync(primary.GetAsync(replacement.Id, TestContext.Current.CancellationToken).AsTask()));
        Assert.Same(replacement, await fixture.RunAsync(secondary.GetAsync(replacement.Id, TestContext.Current.CancellationToken).AsTask()));
        Owned secondRemoval = resources.Last(resource => !ReferenceEquals(resource, removed));
        Assert.True(await fixture.RunAsync(secondary.RemoveAsync(secondRemoval.Id, TestContext.Current.CancellationToken).AsTask()));
        Assert.Equal(1, secondRemoval.DisposeCalls);
        Assert.Equal(2, fixture.Cache.Statistics.Count);
        Assert.Equal(2, fixture.Observer.Removed);
        Owned survivor = Assert.Single(resources, resource => !ReferenceEquals(resource, removed)
            && !ReferenceEquals(resource, secondRemoval));
        Assert.Equal(0, survivor.DisposeCalls);
        Assert.Same(survivor, await fixture.RunAsync(primary.GetAsync(survivor.Id, TestContext.Current.CancellationToken).AsTask()));
        Assert.Same(survivor, await fixture.RunAsync(secondary.GetAsync(survivor.Id, TestContext.Current.CancellationToken).AsTask()));
        Assert.Same(replacement, await fixture.RunAsync(primary.GetAsync(replacement.Id, TestContext.Current.CancellationToken).AsTask()));
        Assert.Same(replacement, await fixture.RunAsync(secondary.GetAsync(replacement.Id, TestContext.Current.CancellationToken).AsTask()));
        var snapshot = fixture.Cache.GetValues(TestContext.Current.CancellationToken);
        Assert.Equal(2, snapshot.Count);
        Assert.Contains(snapshot, resource => ReferenceEquals(resource, survivor));
        Assert.Contains(snapshot, resource => ReferenceEquals(resource, replacement));
        Assert.Equal(4, fixture.Cache.Statistics.TotalCreated);
        Assert.Equal(4, fixture.Observer.Added);
        Assert.Equal(0, fixture.Cache.Statistics.PendingCreations);
        await fixture.RunAsync(fixture.Cache.ClearAsync(TestContext.Current.CancellationToken).AsTask());
        Assert.Empty(fixture.Cache.GetValues(TestContext.Current.CancellationToken));
        Assert.Equal(0, fixture.Cache.Statistics.Count);
        foreach (Owned resource in resources) Assert.Equal(1, resource.DisposeCalls);
        Assert.Equal(1, replacement.DisposeCalls);
        var afterClear = fixture.New("after-clear");
        await fixture.RunAsync(fixture.Cache.AddAsync(afterClear, TestContext.Current.CancellationToken).AsTask());
        Assert.Same(afterClear, await fixture.RunAsync(secondary.GetAsync(afterClear.Id, TestContext.Current.CancellationToken).AsTask()));
        await fixture.StopAsync();
        foreach (Owned resource in resources) Assert.Equal(1, resource.DisposeCalls);
        Assert.Equal(1, replacement.DisposeCalls);
        Assert.Equal(1, afterClear.DisposeCalls);
    }

    sealed class ScanClock : TimeProvider
    {
        TimerCallback? _callback;
        object? _state;
        bool _hostile;
        public FakeTimeProvider Time { get; } = new(DateTimeOffset.UnixEpoch);
        public int Reads { get; private set; }
        public Exception Failure { get; } = new InvalidOperationException("chosen partial expiration clock failure");
        public override long TimestampFrequency
        {
            get
            {
                Reads++;
                if (_hostile && Reads == 3) throw Failure;
                return Time.TimestampFrequency;
            }
        }
        public void Arm(bool hostile) { _hostile = hostile; Reads = 0; }
        public void Fire() => (_callback ?? throw new InvalidOperationException("Cleanup timer was not registered."))(_state);
        public override long GetTimestamp() => Time.GetTimestamp();
        public override DateTimeOffset GetUtcNow() => Time.GetUtcNow();
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            _callback = callback;
            _state = state;
            return Time.CreateTimer(callback, state, dueTime, period);
        }
    }

    sealed class Comparer : IEqualityComparer<string>
    {
        public string? ArmedKey { get; set; }
        public bool Collisions { get; set; }
        public bool ThrowEquality { get; set; }
        public Exception Failure { get; } = new InvalidOperationException("chosen stable comparer failure");
        public int Throws { get; private set; }
        public int GetHashCode(string key)
        {
            if (key == ArmedKey) { Throws++; throw Failure; }
            return Collisions ? int.MinValue : StringComparer.Ordinal.GetHashCode(key);
        }
        public bool Equals(string? left, string? right)
        {
            if (ThrowEquality) { Throws++; throw Failure; }
            return StringComparer.Ordinal.Equals(left, right);
        }
    }

    sealed class BulkComparer(ITestOutputHelper output) : IEqualityComparer<string>
    {
        public bool FailSecondHash { get; set; }
        public bool ThrowFirst { get; set; }
        public int SecondCalls { get; private set; }
        public int Throws { get; private set; }
        public Exception Failure { get; } = new InvalidOperationException("chosen bulk index comparer failure");
        public int GetHashCode(string key)
        {
            if (key == "second") SecondCalls++;
            if ((FailSecondHash && key == "second" && SecondCalls == 2) || (ThrowFirst && key == "first"))
            {
                Throws++;
                output.WriteLine("ACTUAL_BULK_COMPARER_ATTEMPT: " + key + "\n" + new System.Diagnostics.StackTrace());
                throw Failure;
            }
            return StringComparer.Ordinal.GetHashCode(key);
        }
        public bool Equals(string? left, string? right) => StringComparer.Ordinal.Equals(left, right);
    }

    sealed class Owned(string id) : IAsyncDisposable
    {
        public string Id { get; } = id;
        public int DisposeCalls { get; private set; }
        public ValueTask DisposeAsync() { DisposeCalls++; return ValueTask.CompletedTask; }
    }

    sealed class Observer : IResourceCacheObserver<Owned>
    {
        public int Added { get; private set; }
        public int Removed { get; private set; }
        public ValueTask ResourceAddedAsync(Owned value, CancellationToken token) { Added++; return ValueTask.CompletedTask; }
        public ValueTask ResourceRemovedAsync(Owned value, CancellationToken token) { Removed++; return ValueTask.CompletedTask; }
        public ValueTask CacheClearedAsync(CancellationToken token) => ValueTask.CompletedTask;
    }

    sealed class Fixture : IAsyncDisposable
    {
        readonly List<Owned> _resources = [];
        readonly List<Task> _operations = [];
        readonly Dictionary<Task, Exception> _observedFailures = [];
        readonly ConnectHandle _observerHandle;
        Task? _stop;
        public FakeTimeProvider Time { get; } = new(DateTimeOffset.UnixEpoch);
        public Observer Observer { get; } = new();
        public ResourceCache<Owned> Cache { get; }
        public Fixture(int capacity = 1, TimeProvider? timeProvider = null)
        {
            Cache = new(new ResourceCacheOptions(capacity: capacity, maxAge: TimeSpan.FromSeconds(9),
                expirationMode: ResourceCacheExpirationMode.Absolute, timeProvider: timeProvider ?? Time, cleanupInterval: TimeSpan.FromDays(1)));
            _observerHandle = Cache.Connect(Observer);
        }
        public TTask Own<TTask>(TTask operation) where TTask : Task
        {
            if (!_operations.Contains(operation)) _operations.Add(operation);
            return operation;
        }
        public async Task<T> RunAsync<T>(Task<T> operation)
        {
            _ = Own(operation);
            try { return await operation.WaitAsync(Bound, TestContext.Current.CancellationToken); }
            catch (Exception failure) when (operation.IsCompleted && failure is not TimeoutException)
            { _observedFailures[operation] = failure; throw; }
        }
        public async Task RunAsync(Task operation)
        {
            _ = Own(operation);
            try { await operation.WaitAsync(Bound, TestContext.Current.CancellationToken); }
            catch (Exception failure) when (operation.IsCompleted && failure is not TimeoutException)
            { _observedFailures[operation] = failure; throw; }
        }
        public async Task<(Owned? Value, Exception? Failure)> ReadAsync(IResourceCacheIndex<string, Owned> index, string key)
        {
            Owned? value = null;
            Exception? failure = await Record.ExceptionAsync(async () => value = await RunAsync(index.GetAsync(key, TestContext.Current.CancellationToken).AsTask()));
            return (value, failure);
        }
        async Task ObserveAllAsync(int index)
        {
            if (index == _operations.Count) return;
            Task operation = _operations[index];
            try
            {
                try { await operation.WaitAsync(Bound, CancellationToken.None); }
                catch (Exception failure) when (operation.IsCompleted && failure is not TimeoutException
                    && _observedFailures.TryGetValue(operation, out Exception? recorded) && ReferenceEquals(recorded, failure))
                { /* Observe only the exact terminal outcome already captured by this fixture. */ }
            }
            finally { await ObserveAllAsync(index + 1); }
        }
        public Owned New(string id) { var resource = new Owned(id); _resources.Add(resource); return resource; }
        public async Task StopAsync()
        {
            _stop ??= Cache.DisposeAsync().AsTask();
            await _stop.WaitAsync(Bound, CancellationToken.None);
        }
        public async ValueTask DisposeAsync()
        {
            try { await ObserveAllAsync(0); }
            finally
            {
                try { await StopAsync(); }
                finally
                {
                    _observerHandle.Disconnect();
                    // Fixture rescue is permitted only after actual cache owner termination.
                    // Counts were asserted before this rescue and confer no product-release credit.
                    if (_stop?.IsCompleted == true && _operations.All(operation => operation.IsCompleted))
                        foreach (Owned resource in _resources)
                            if (resource.DisposeCalls == 0) await resource.DisposeAsync();
                }
            }
        }
    }
}
