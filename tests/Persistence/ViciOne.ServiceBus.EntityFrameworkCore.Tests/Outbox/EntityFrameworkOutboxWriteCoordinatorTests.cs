using System.Buffers;
using System.Net.Mime;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Outbox;

public sealed class EntityFrameworkOutboxWriteCoordinatorTests
{
    private static readonly DateTimeOffset Now =
        new(2042, 3, 4, 5, 6, 7, TimeSpan.Zero);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "rejected-later-admission-leaves-earlier-intent-under-explicit-session-ownership")]
    public async Task RejectedLaterAdmission_ExplicitAbortOrCommitOwnsEarlierIntentAsync(bool commitEarlier)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync(maximumEnvelopeBytes: 16384);
        Guid firstId = Guid.NewGuid();
        Guid rejectedId = Guid.NewGuid();
        Guid replacementId = Guid.NewGuid();
        using (EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext())
        {
            await context.AddSendAsync(CreateSendContext(firstId, 1), token);
            MessageSendContext<OutboxProbe> rejected = CreateSendContext(rejectedId, 2);
            rejected.Headers.Set("oversized", new string('x', 65536));

            PayloadAdmissionException failure = await Assert.ThrowsAsync<PayloadAdmissionException>(() =>
                context.AddSendAsync(rejected, token));

            Assert.Equal(16384, failure.ConfiguredLimitBytes);
            Assert.True(failure.ActualBytes > failure.ConfiguredLimitBytes);
            Assert.Equal(firstId, Assert.Single(fixture.DbContext.ChangeTracker.Entries<DurableSendRecord>()).Entity.Id);
            Assert.Equal(1, Assert.Single(fixture.DbContext.ChangeTracker.Entries<DurableSendCapacityState>()).Entity.StoredCount);
            if (commitEarlier)
                await context.CommitAsync(token);
            else
            {
                await context.AbortAsync(token);
                await fixture.DbContext.SaveChangesAsync(token);
            }
        }

        await using (OutboxDbContext afterDecision = fixture.CreateFreshContext())
        {
            DurableSendRecord[] retained = await afterDecision.Set<DurableSendRecord>().AsNoTracking().ToArrayAsync(token);
            Guid[] expectedRetained = commitEarlier ? [firstId] : [];
            Assert.Equal(expectedRetained, retained.Select(x => x.Id));
            Assert.DoesNotContain(retained, x => x.Id == rejectedId);
            DurableSendCapacityState capacity = await afterDecision.Set<DurableSendCapacityState>()
                .AsNoTracking().SingleAsync(token);
            Assert.Equal(retained.Length, capacity.StoredCount);
            Assert.Equal(retained.Sum(x => x.StorageSize), capacity.StoredBytes);
        }

        using (EntityFrameworkScopedBusContext<IBus, OutboxDbContext> next = fixture.CreateBusContext())
        {
            await next.AddSendAsync(CreateSendContext(replacementId, 3), token);
            await next.CommitAsync(token);
        }

        await using OutboxDbContext final = fixture.CreateFreshContext();
        DurableSendRecord[] messages = await final.Set<DurableSendRecord>().AsNoTracking().ToArrayAsync(token);
        Guid[] expectedFinal = commitEarlier ? [firstId, replacementId] : [replacementId];
        Assert.Equal(expectedFinal.Order(), messages.Select(x => x.Id).Order());
        Assert.DoesNotContain(messages, x => x.Id == rejectedId);
        DurableSendCapacityState finalCapacity = await final.Set<DurableSendCapacityState>().AsNoTracking().SingleAsync(token);
        Assert.Equal(messages.Length, finalCapacity.StoredCount);
        Assert.Equal(messages.Sum(x => x.StorageSize), finalCapacity.StoredBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t115-tracking-failure-detaches-rejected-send-and-preserves-session")]
    public async Task TrackingFailure_DetachesRejectedSendAndPreservesHealthySessionAsync(bool hasEarlierSend)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        Guid earlierId = Guid.NewGuid();
        Guid rejectedId = Guid.NewGuid();
        Guid recoveredId = Guid.NewGuid();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        if (hasEarlierSend)
            await context.AddSendAsync(CreateSendContext(earlierId, 1), token);

        var sentinel = new InvalidOperationException("record tracked callback failed");
        bool throwOnce = true;
        fixture.DbContext.ChangeTracker.Tracked += (_, args) =>
        {
            if (throwOnce && args.Entry.Entity is DurableSendRecord record && record.Id == rejectedId)
            {
                throwOnce = false;
                throw sentinel;
            }
        };

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.AddSendAsync(CreateSendContext(rejectedId, 2), token));

        Assert.Same(sentinel, failure);
        Assert.DoesNotContain(fixture.DbContext.ChangeTracker.Entries<DurableSendRecord>(),
            entry => entry.Entity.Id == rejectedId);
        Guid[] expectedAfterFailure = hasEarlierSend ? [earlierId] : [];
        Assert.Equal(expectedAfterFailure,
            fixture.DbContext.ChangeTracker.Entries<DurableSendRecord>().Select(entry => entry.Entity.Id));
        DurableSendCapacityState capacity = Assert.Single(fixture.DbContext.ChangeTracker.Entries<DurableSendCapacityState>()).Entity;
        Assert.Equal(hasEarlierSend ? 1 : 0, capacity.StoredCount);
        Assert.Equal(fixture.DbContext.ChangeTracker.Entries<DurableSendRecord>().Sum(entry => entry.Entity.StorageSize),
            capacity.StoredBytes);

        await context.CommitAsync(token);
        await context.AddSendAsync(CreateSendContext(recoveredId, 3), token);
        await context.CommitAsync(token);

        await using OutboxDbContext persisted = fixture.CreateFreshContext();
        DurableSendRecord[] messages = await persisted.Set<DurableSendRecord>().AsNoTracking().ToArrayAsync(token);
        Guid[] expectedPersisted = hasEarlierSend ? [earlierId, recoveredId] : [recoveredId];
        Assert.Equal(expectedPersisted,
            messages.Select(record => record.Id));
        Assert.DoesNotContain(messages, record => record.Id == rejectedId);
        DurableSendCapacityState storedCapacity = await persisted.Set<DurableSendCapacityState>()
            .AsNoTracking().SingleAsync(token);
        Assert.Equal(messages.Length, storedCapacity.StoredCount);
        Assert.Equal(messages.Sum(record => record.StorageSize), storedCapacity.StoredBytes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t115-detach-callback-failure-preserves-primary-error-and-capacity")]
    public async Task DetachCallbackFailure_PreservesPrimaryErrorAndAllowsHealthyRetryAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        Guid rejectedId = Guid.NewGuid();
        Guid recoveredId = Guid.NewGuid();
        var primary = new InvalidOperationException("record tracking failed");
        var secondary = new InvalidOperationException("record detach callback failed");
        bool throwOnTrack = true;
        bool throwOnDetach = true;
        fixture.DbContext.ChangeTracker.Tracked += (_, args) =>
        {
            if (throwOnTrack && args.Entry.Entity is DurableSendRecord record && record.Id == rejectedId)
            {
                throwOnTrack = false;
                throw primary;
            }
        };
        fixture.DbContext.ChangeTracker.StateChanged += (_, args) =>
        {
            if (throwOnDetach && args.Entry.Entity is DurableSendRecord record
                && record.Id == rejectedId && args.NewState == EntityState.Detached)
            {
                throwOnDetach = false;
                throw secondary;
            }
        };

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.AddSendAsync(CreateSendContext(rejectedId, 1), token));

        Assert.Same(primary, failure);
        Assert.False(throwOnDetach);
        Assert.DoesNotContain(fixture.DbContext.ChangeTracker.Entries<DurableSendRecord>(),
            entry => entry.Entity.Id == rejectedId);
        DurableSendCapacityState capacity = Assert.Single(fixture.DbContext.ChangeTracker.Entries<DurableSendCapacityState>()).Entity;
        Assert.Equal(0, capacity.StoredCount);
        Assert.Equal(0, capacity.StoredBytes);
        await context.AddSendAsync(CreateSendContext(recoveredId, 2), token);
        await context.CommitAsync(token);

        await using OutboxDbContext persisted = fixture.CreateFreshContext();
        DurableSendRecord stored = await persisted.Set<DurableSendRecord>().AsNoTracking().SingleAsync(token);
        Assert.Equal(recoveredId, stored.Id);
        DurableSendCapacityState storedCapacity = await persisted.Set<DurableSendCapacityState>()
            .AsNoTracking().SingleAsync(token);
        Assert.Equal(1, storedCapacity.StoredCount);
        Assert.Equal(stored.StorageSize, storedCapacity.StoredBytes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t115-detect-changes-failure-cannot-leave-rejected-send-tracked")]
    public async Task DetectChangesFailure_CannotLeaveRejectedSendTrackedOrPersistedAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        Guid rejectedId = Guid.NewGuid();
        Guid recoveredId = Guid.NewGuid();
        var primary = new InvalidOperationException("record tracking failed");
        var secondary = new InvalidOperationException("detect changes callback failed");
        bool trackingFailed = false;
        fixture.DbContext.ChangeTracker.Tracked += (_, args) =>
        {
            if (args.Entry.Entity is DurableSendRecord record && record.Id == rejectedId)
            {
                trackingFailed = true;
                throw primary;
            }
        };
        fixture.DbContext.ChangeTracker.DetectingAllChanges += (_, _) =>
        {
            if (trackingFailed)
                throw secondary;
        };

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.AddSendAsync(CreateSendContext(rejectedId, 1), token));

        fixture.DbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        Assert.Same(primary, failure);
        Assert.True(trackingFailed);
        Assert.DoesNotContain(fixture.DbContext.ChangeTracker.Entries<DurableSendRecord>(),
            entry => entry.Entity.Id == rejectedId);
        DurableSendCapacityState capacity = Assert.Single(fixture.DbContext.ChangeTracker.Entries<DurableSendCapacityState>()).Entity;
        Assert.Equal(0, capacity.StoredCount);
        Assert.Equal(0, capacity.StoredBytes);

        await context.AddSendAsync(CreateSendContext(recoveredId, 2), token);
        await context.CommitAsync(token);
        await using OutboxDbContext persisted = fixture.CreateFreshContext();
        Assert.Equal(recoveredId,
            (await persisted.Set<DurableSendRecord>().AsNoTracking().SingleAsync(token)).Id);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t115-pre-detach-callback-failure-cannot-leave-rejected-send-tracked")]
    public async Task StateChangingFailure_CannotLeaveRejectedSendTrackedOrPersistedAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        Guid rejectedId = Guid.NewGuid();
        Guid recoveredId = Guid.NewGuid();
        var primary = new InvalidOperationException("record tracking failed");
        var secondary = new InvalidOperationException("pre-detach callback failed");
        bool throwOnTrack = true;
        bool throwOnDetach = true;
        fixture.DbContext.ChangeTracker.Tracked += (_, args) =>
        {
            if (throwOnTrack && args.Entry.Entity is DurableSendRecord record && record.Id == rejectedId)
            {
                throwOnTrack = false;
                throw primary;
            }
        };
        fixture.DbContext.ChangeTracker.StateChanging += (_, args) =>
        {
            if (throwOnDetach && args.Entry.Entity is DurableSendRecord record
                && record.Id == rejectedId && args.NewState == EntityState.Detached)
            {
                throwOnDetach = false;
                throw secondary;
            }
        };

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.AddSendAsync(CreateSendContext(rejectedId, 1), token));

        Assert.Same(primary, failure);
        Assert.False(throwOnDetach);
        Assert.DoesNotContain(fixture.DbContext.ChangeTracker.Entries<DurableSendRecord>(),
            entry => entry.Entity.Id == rejectedId);
        DurableSendCapacityState capacity = Assert.Single(fixture.DbContext.ChangeTracker.Entries<DurableSendCapacityState>()).Entity;
        Assert.Equal(0, capacity.StoredCount);
        Assert.Equal(0, capacity.StoredBytes);
        await context.AddSendAsync(CreateSendContext(recoveredId, 2), token);
        await context.CommitAsync(token);

        await using OutboxDbContext persisted = fixture.CreateFreshContext();
        Assert.Equal(recoveredId,
            (await persisted.Set<DurableSendRecord>().AsNoTracking().SingleAsync(token)).Id);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t115-persistent-pre-detach-callback-fails-closed-without-orphan")]
    public async Task PersistentStateChangingFailure_ClearsRejectedTrackerAndAllowsRetryAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        Guid rejectedId = Guid.NewGuid();
        Guid recoveredId = Guid.NewGuid();
        var primary = new InvalidOperationException("record tracking failed");
        int detachAttempts = 0;
        var pendingBusiness = new BusinessRecord(Guid.NewGuid(), "must be replayed after tracker failure");
        fixture.DbContext.Add(pendingBusiness);
        fixture.DbContext.ChangeTracker.Tracked += (_, args) =>
        {
            if (args.Entry.Entity is DurableSendRecord record && record.Id == rejectedId)
                throw primary;
        };
        fixture.DbContext.ChangeTracker.StateChanging += (_, args) =>
        {
            if (args.Entry.Entity is DurableSendRecord record
                && record.Id == rejectedId && args.NewState == EntityState.Detached)
            {
                detachAttempts++;
                throw new InvalidOperationException("detach forbidden");
            }
        };

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.AddSendAsync(CreateSendContext(rejectedId, 1), token));

        Assert.Same(primary, failure.InnerException);
        Assert.Contains("All pending changes in this DbContext were discarded", failure.Message, StringComparison.Ordinal);
        Assert.Equal(2, detachAttempts);
        Assert.Empty(fixture.DbContext.ChangeTracker.Entries<DurableSendRecord>());
        Assert.Empty(fixture.DbContext.ChangeTracker.Entries<DurableSendCapacityState>());
        Assert.Empty(fixture.DbContext.ChangeTracker.Entries<BusinessRecord>());
        await context.AddSendAsync(CreateSendContext(recoveredId, 2), token);
        await context.CommitAsync(token);

        await using OutboxDbContext persisted = fixture.CreateFreshContext();
        DurableSendRecord stored = await persisted.Set<DurableSendRecord>().AsNoTracking().SingleAsync(token);
        Assert.Equal(recoveredId, stored.Id);
        DurableSendCapacityState storedCapacity = await persisted.Set<DurableSendCapacityState>()
            .AsNoTracking().SingleAsync(token);
        Assert.Equal(1, storedCapacity.StoredCount);
        Assert.Equal(stored.StorageSize, storedCapacity.StoredBytes);
        Assert.Empty(await persisted.Set<BusinessRecord>().AsNoTracking().ToArrayAsync(token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-WRITE-COORDINATOR", "required-actions-fail-at-boundary")]
    public async Task Coordinator_RejectsMissingActionsAsync()
    {
        using var coordinator = new EntityFrameworkOutboxWriteCoordinator();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            coordinator.ExecuteAsync(null!, TestContext.Current.CancellationToken));
        Assert.Throws<ArgumentNullException>(() => coordinator.ExecuteSynchronous(null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-WRITE-COORDINATOR", "synchronous-disposal-cannot-race-an-async-write")]
    public async Task Coordinator_RejectsSynchronousMutationWhileAsyncWorkOwnsTheSessionAsync()
    {
        using var coordinator = new EntityFrameworkOutboxWriteCoordinator();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task operation = coordinator.ExecuteAsync(
            async () =>
            {
                entered.SetResult();
                await release.Task;
            },
            TestContext.Current.CancellationToken);
        await entered.Task;

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            coordinator.ExecuteSynchronous(() => { }));
        release.SetResult();
        await operation;

        Assert.Contains("busy", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "commit-persists-business-and-intent")]
    public async Task Commit_PersistsBusinessDataAndOutboxIntentThroughTheSameContextAsync()
    {
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        var business = new BusinessRecord(Guid.NewGuid(), "committed");
        fixture.DbContext.Add(business);
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), TestContext.Current.CancellationToken);

        await context.CommitAsync(TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();

        Assert.Equal(business, await fixture.DbContext.Set<BusinessRecord>().SingleAsync(TestContext.Current.CancellationToken));
        DurableSendRecord message = await fixture.DbContext.Set<DurableSendRecord>()
            .SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("default", message.StoreKey);
        Assert.Equal(DurableSendStatus.Pending, message.Status);
        Assert.Equal("vicione.tests.transactional-outbox;v=1", message.ContractIdentity);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "commit-without-message-persists-business")]
    public async Task Commit_WithoutAStagedMessageStillPersistsBusinessChangesAsync()
    {
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        var business = new BusinessRecord(Guid.NewGuid(), "business-only");
        fixture.DbContext.Add(business);

        await context.CommitAsync(TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();

        Assert.Equal(business, await fixture.DbContext.Set<BusinessRecord>().SingleAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.DbContext.Set<DurableSendRecord>().ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t100-cleared-tracker-cannot-claim-staged-send-was-committed")]
    public async Task Commit_AfterTrackerClearRejectsLostIntentAndAllowsAbortAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        Guid messageId = Guid.NewGuid();
        await context.AddSendAsync(CreateSendContext(messageId, 1), token);
        fixture.DbContext.ChangeTracker.Clear();

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => context.CommitAsync(token));

        Assert.Contains("staged", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(context.HasActiveSession);
        await context.AbortAsync(token);
        Assert.False(context.HasActiveSession);
        await using OutboxDbContext persisted = fixture.CreateFreshContext();
        Assert.Empty(await persisted.Set<DurableSendRecord>().AsNoTracking().ToArrayAsync(token));
        Assert.Empty(await persisted.Set<DurableSendCapacityState>().AsNoTracking().ToArrayAsync(token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t100-detached-staged-send-releases-capacity-without-losing-business-state")]
    public async Task Abort_AfterStagedRecordDetachmentReleasesCapacityAndPreservesBusinessStateAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        var business = new BusinessRecord(Guid.NewGuid(), "retained after detachment");
        fixture.DbContext.Add(business);
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), token);
        DurableSendRecord staged = Assert.Single(fixture.DbContext.Set<DurableSendRecord>().Local);
        long stagedBytes = staged.StorageSize;
        fixture.DbContext.Entry(staged).State = EntityState.Detached;

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => context.CommitAsync(token));

        Assert.Contains("staged", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(context.HasActiveSession);
        await context.AbortAsync(token);
        Assert.False(context.HasActiveSession);
        await fixture.DbContext.SaveChangesAsync(token);
        await using OutboxDbContext persisted = fixture.CreateFreshContext();
        Assert.Equal(business, await persisted.Set<BusinessRecord>().AsNoTracking().SingleAsync(token));
        Assert.Empty(await persisted.Set<DurableSendRecord>().AsNoTracking().ToArrayAsync(token));
        DurableSendCapacityState capacity = await persisted.Set<DurableSendCapacityState>().AsNoTracking().SingleAsync(token);
        Assert.Equal(0, capacity.StoredCount);
        Assert.Equal(0, capacity.StoredBytes);
        Assert.True(stagedBytes > 0);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "transactional-outbox-rejects-empty-message-id-before-staging")]
    public async Task EmptyMessageId_RejectsBeforeStagingAndPreservesTheSessionAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();

        try
        {
            MessageException failure = await Assert.ThrowsAsync<MessageException>(() =>
                context.AddSendAsync(CreateSendContext(Guid.Empty, 1), token));
            Assert.Contains("nonempty", failure.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(context.HasActiveSession);
            Assert.Empty(fixture.DbContext.ChangeTracker.Entries<DurableSendRecord>());
            Assert.Empty(fixture.DbContext.ChangeTracker.Entries<DurableSendCapacityState>());
        }
        finally
        {
            if (context.HasActiveSession)
                await context.AbortAsync(token);
        }

        Guid validId = Guid.NewGuid();
        await context.AddSendAsync(CreateSendContext(validId, 2), token);
        await context.CommitAsync(token);

        await using OutboxDbContext persisted = fixture.CreateFreshContext();
        DurableSendRecord stored = await persisted.Set<DurableSendRecord>().AsNoTracking().SingleAsync(token);
        DurableSendCapacityState capacity = await persisted.Set<DurableSendCapacityState>().AsNoTracking().SingleAsync(token);
        Assert.Equal(validId, stored.Id);
        Assert.Equal(validId, stored.MessageId);
        Assert.Equal(1, capacity.StoredCount);
        Assert.Equal(stored.StorageSize, capacity.StoredBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "serializer-cannot-change-staged-message-identity")]
    public async Task SerializerChangingMessageId_RejectsWithoutStagingAndAllowsValidCommitAsync(bool clearId)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        Guid initialId = Guid.NewGuid();
        Guid replacementId = clearId ? Guid.Empty : Guid.NewGuid();
        MessageSendContext<OutboxProbe> changed = CreateSendContext(initialId, 1);
        changed.Serializer = new MessageIdChangingSerializer(replacementId);

        try
        {
            MessageException failure = await Assert.ThrowsAsync<MessageException>(() => context.AddSendAsync(changed, token));
            Assert.Contains("changed during serialization", failure.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(replacementId, changed.MessageId);
            Assert.False(context.HasActiveSession);
            Assert.Empty(fixture.DbContext.ChangeTracker.Entries<DurableSendRecord>());
            Assert.Empty(fixture.DbContext.ChangeTracker.Entries<DurableSendCapacityState>());
        }
        finally
        {
            if (context.HasActiveSession)
                await context.AbortAsync(token);
        }

        Guid validId = Guid.NewGuid();
        await context.AddSendAsync(CreateSendContext(validId, 2), token);
        await context.CommitAsync(token);

        await using OutboxDbContext persisted = fixture.CreateFreshContext();
        DurableSendRecord stored = await persisted.Set<DurableSendRecord>().AsNoTracking().SingleAsync(token);
        DurableSendCapacityState capacity = await persisted.Set<DurableSendCapacityState>().AsNoTracking().SingleAsync(token);
        Assert.Equal(validId, stored.Id);
        Assert.Equal(validId, stored.MessageId);
        Assert.NotEqual(initialId, stored.Id);
        Assert.NotEqual(replacementId, stored.Id);
        Assert.Equal(1, capacity.StoredCount);
        Assert.Equal(stored.StorageSize, capacity.StoredBytes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t100-duplicate-message-id-does-not-reserve-capacity-twice")]
    public async Task DuplicateMessageId_RejectsSecondAdmissionWithoutInflatingCommittedCapacityAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        Guid id = Guid.NewGuid();
        await context.AddSendAsync(CreateSendContext(id, 1), token);
        DurableSendRecord first = Assert.Single(fixture.DbContext.Set<DurableSendRecord>().Local);

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.AddSendAsync(CreateSendContext(id, 2), token));

        Assert.Contains("staged", failure.Message, StringComparison.OrdinalIgnoreCase);
        DurableSendCapacityState pendingCapacity = Assert.Single(fixture.DbContext.Set<DurableSendCapacityState>().Local);
        Assert.Equal(1, pendingCapacity.StoredCount);
        Assert.Equal(first.StorageSize, pendingCapacity.StoredBytes);
        await context.CommitAsync(token);
        await using OutboxDbContext persisted = fixture.CreateFreshContext();
        DurableSendRecord stored = await persisted.Set<DurableSendRecord>().AsNoTracking().SingleAsync(token);
        DurableSendCapacityState capacity = await persisted.Set<DurableSendCapacityState>().AsNoTracking().SingleAsync(token);
        Assert.Equal(id, stored.Id);
        Assert.Equal(1, capacity.StoredCount);
        Assert.Equal(stored.StorageSize, capacity.StoredBytes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t100-accept-all-changes-cannot-claim-unsaved-intent")]
    public async Task AcceptAllChanges_WithoutDatabaseSaveCannotCompleteTheOutboxSessionAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), token);
        fixture.DbContext.ChangeTracker.AcceptAllChanges();

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => context.CommitAsync(token));

        Assert.Contains("staged", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(context.HasActiveSession);
        await context.AbortAsync(token);
        Assert.False(context.HasActiveSession);
        var business = new BusinessRecord(Guid.NewGuid(), "saved after abandoned intent");
        fixture.DbContext.Add(business);
        await fixture.DbContext.SaveChangesAsync(token);
        await using OutboxDbContext persisted = fixture.CreateFreshContext();
        Assert.Equal(business, await persisted.Set<BusinessRecord>().AsNoTracking().SingleAsync(token));
        Assert.Empty(await persisted.Set<DurableSendRecord>().AsNoTracking().ToArrayAsync(token));
        DurableSendCapacityState capacity = await persisted.Set<DurableSendCapacityState>().AsNoTracking().SingleAsync(token);
        Assert.Equal((0, 0L), (capacity.StoredCount, capacity.StoredBytes));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t100-foreign-store-record-with-same-id-cannot-complete-own-session")]
    public async Task ForeignStoreRecordWithSameId_CannotBeMistakenForACommittedOwnRecordAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        Guid id = Guid.NewGuid();
        await context.AddSendAsync(CreateSendContext(id, 1), token);
        DurableSendRecord own = Assert.Single(fixture.DbContext.Set<DurableSendRecord>().Local);
        var foreign = new DurableSendRecord
        {
            StoreKey = "foreign-store",
            Id = id,
            GenerationToken = Guid.NewGuid(),
            ContractIdentity = own.ContractIdentity,
            DestinationAddress = own.DestinationAddress,
            ContentType = own.ContentType,
            Body = own.Body,
            StorageSize = own.StorageSize,
            Status = DurableSendStatus.Pending,
            EnqueuedAt = own.EnqueuedAt,
        };
        fixture.DbContext.Attach(foreign);

        await context.CommitAsync(token);

        Assert.Equal(EntityState.Unchanged, fixture.DbContext.Entry(foreign).State);
        Assert.False(context.HasActiveSession);
        await using OutboxDbContext persisted = fixture.CreateFreshContext();
        DurableSendRecord stored = await persisted.Set<DurableSendRecord>().AsNoTracking().SingleAsync(token);
        Assert.Equal(("default", id, own.GenerationToken), (stored.StoreKey, stored.Id, stored.GenerationToken));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t100-suppressed-save-cannot-complete-unsaved-intent")]
    public async Task SuppressedSaveChanges_CannotCompleteAnUnpersistedOutboxSessionAsync(int reportedSavedCount)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var interceptor = new SuppressingSaveInterceptor();
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync(interceptor: interceptor);
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), token);
        interceptor.Suppress = true;
        interceptor.ReportedSavedCount = reportedSavedCount;

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => context.CommitAsync(token));

        Assert.Contains("did not persist", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(context.HasActiveSession);
        interceptor.Suppress = false;
        await context.AbortAsync(token);
        var business = new BusinessRecord(Guid.NewGuid(), "saved after suppression");
        fixture.DbContext.Add(business);
        await fixture.DbContext.SaveChangesAsync(token);
        await using OutboxDbContext persisted = fixture.CreateFreshContext();
        Assert.Equal(business, await persisted.Set<BusinessRecord>().AsNoTracking().SingleAsync(token));
        Assert.Empty(await persisted.Set<DurableSendRecord>().AsNoTracking().ToArrayAsync(token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t100-persisted-tracked-id-collision-preserves-capacity")]
    public async Task PersistedTrackedMessageIdCollision_RejectsBeforeReservingCapacityAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        Guid id = Guid.NewGuid();
        await context.AddSendAsync(CreateSendContext(id, 1), token);
        await context.CommitAsync(token);
        DurableSendCapacityState capacity = Assert.Single(fixture.DbContext.Set<DurableSendCapacityState>().Local);
        long originalBytes = capacity.StoredBytes;

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.AddSendAsync(CreateSendContext(id, 2), token));

        Assert.Contains("already staged", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(context.HasActiveSession);
        Assert.Equal((1, originalBytes), (capacity.StoredCount, capacity.StoredBytes));
        await using OutboxDbContext persisted = fixture.CreateFreshContext();
        DurableSendRecord stored = await persisted.Set<DurableSendRecord>().AsNoTracking().SingleAsync(token);
        Assert.Equal(id, stored.Id);
        DurableSendCapacityState storedCapacity = await persisted.Set<DurableSendCapacityState>().AsNoTracking().SingleAsync(token);
        Assert.Equal((1, originalBytes), (storedCapacity.StoredCount, storedCapacity.StoredBytes));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t100-external-save-without-acceptance-retains-durable-ledger")]
    public async Task ExternalSaveWithoutAcceptance_DoesNotReinsertOrRollBackPersistedIntentAsync(bool abort)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        Guid id = Guid.NewGuid();
        await context.AddSendAsync(CreateSendContext(id, 1), token);
        await fixture.DbContext.SaveChangesAsync(acceptAllChangesOnSuccess: false, token);

        if (abort)
            await context.AbortAsync(token);
        else
            await context.CommitAsync(token);

        await context.CommitAsync(token);
        Guid nextId = Guid.NewGuid();
        await context.AddSendAsync(CreateSendContext(nextId, 2), token);
        await context.CommitAsync(token);

        await using OutboxDbContext persisted = fixture.CreateFreshContext();
        DurableSendRecord[] records = await persisted.Set<DurableSendRecord>().AsNoTracking().ToArrayAsync(token);
        DurableSendCapacityState capacity = await persisted.Set<DurableSendCapacityState>().AsNoTracking().SingleAsync(token);
        Assert.Equal(new[] { id, nextId }.Order(), records.Select(record => record.Id).Order());
        Assert.Equal((2, records.Sum(record => record.StorageSize)), (capacity.StoredCount, capacity.StoredBytes));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t100-external-save-without-acceptance-preserves-business-entry-state")]
    public async Task ExternalSaveWithoutAcceptance_AcceptsOnlyOwnedOutboxEntriesAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        var business = new BusinessRecord(Guid.NewGuid(), "caller owned state");
        fixture.DbContext.Add(business);
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), token);

        await fixture.DbContext.SaveChangesAsync(acceptAllChangesOnSuccess: false, token);

        Assert.False(context.HasActiveSession);
        Assert.Equal(EntityState.Added, fixture.DbContext.Entry(business).State);
        Assert.All(fixture.DbContext.ChangeTracker.Entries<DurableSendRecord>(),
            entry => Assert.Equal(EntityState.Unchanged, entry.State));
        Assert.Equal(EntityState.Unchanged,
            Assert.Single(fixture.DbContext.ChangeTracker.Entries<DurableSendCapacityState>()).State);
        await using OutboxDbContext persisted = fixture.CreateFreshContext();
        Assert.Equal(business, await persisted.Set<BusinessRecord>().AsNoTracking().SingleAsync(token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t100-caller-transaction-rollback-removes-outbox-intent")]
    public async Task CallerTransactionRollback_RemovesSavedIntentWithoutPhantomRestagingAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        await using (var transaction = await fixture.DbContext.Database.BeginTransactionAsync(token))
        {
            await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), token);
            await fixture.DbContext.SaveChangesAsync(acceptAllChangesOnSuccess: false, token);
            Assert.False(context.HasActiveSession);
            await transaction.RollbackAsync(token);
        }
        await context.CommitAsync(token);

        await using OutboxDbContext persisted = fixture.CreateFreshContext();
        Assert.Empty(await persisted.Set<DurableSendRecord>().AsNoTracking().ToArrayAsync(token));
        Assert.Empty(await persisted.Set<DurableSendCapacityState>().AsNoTracking().ToArrayAsync(token));
    }

    [Theory]
    [InlineData(EntityState.Unchanged)]
    [InlineData(EntityState.Detached)]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t100-missing-capacity-write-cannot-claim-commit")]
    public async Task MissingCapacityWrite_CannotCommitTheStagedSendAsync(EntityState capacityState)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), token);
        DurableSendCapacityState capacity = Assert.Single(fixture.DbContext.Set<DurableSendCapacityState>().Local);
        fixture.DbContext.Entry(capacity).State = capacityState;

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => context.CommitAsync(token));

        Assert.Contains("capacity", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(context.HasActiveSession);
        await using OutboxDbContext persisted = fixture.CreateFreshContext();
        Assert.Empty(await persisted.Set<DurableSendRecord>().AsNoTracking().ToArrayAsync(token));
        await context.AbortAsync(token);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t100-mutated-staged-size-cannot-corrupt-capacity")]
    public async Task MutatedStagedStorageSize_CannotCommitAnInconsistentCapacityLedgerAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), token);
        DurableSendRecord record = Assert.Single(fixture.DbContext.Set<DurableSendRecord>().Local);
        Assert.True(record.StorageSize > 0);
        record.StorageSize = 0;

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => context.CommitAsync(token));

        Assert.Contains("staged", failure.Message, StringComparison.OrdinalIgnoreCase);
        await using OutboxDbContext persisted = fixture.CreateFreshContext();
        Assert.Empty(await persisted.Set<DurableSendRecord>().AsNoTracking().ToArrayAsync(token));
        await context.AbortAsync(token);
        Assert.False(context.HasActiveSession);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DURABLE-CAPACITY", "transactional-outbox-counts-payload-and-metadata")]
    public async Task StagedMessage_AccountsForPayloadAndMetadataAgainstTheCapacityLimitAsync()
    {
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        MessageSendContext<OutboxProbe> sendContext = CreateSendContext(Guid.NewGuid(), 1);
        sendContext.Headers.Set("x-capacity-probe", new string('x', 128));

        await context.AddSendAsync(sendContext, TestContext.Current.CancellationToken);

        DurableSendRecord message = Assert.Single(fixture.DbContext.Set<DurableSendRecord>().Local);
        DurableSendCapacityState capacity = Assert.Single(fixture.DbContext.Set<DurableSendCapacityState>().Local);
        byte[] metadata = Assert.IsType<byte[]>(message.Metadata);
        long expectedStorageSize = checked(message.Body.LongLength + metadata.LongLength);
        Assert.NotEmpty(metadata);
        Assert.Equal(expectedStorageSize, message.StorageSize);
        Assert.Equal((1, expectedStorageSize), (capacity.StoredCount, capacity.StoredBytes));

        await context.AbortAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "abort-detaches-only-session-intent")]
    public async Task Abort_DetachesOnlyTheSessionOutboxAndRetainsBusinessChangesAsync()
    {
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        var business = new BusinessRecord(Guid.NewGuid(), "retained");
        fixture.DbContext.Add(business);
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), TestContext.Current.CancellationToken);

        await context.AbortAsync(TestContext.Current.CancellationToken);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();

        Assert.Equal(business, await fixture.DbContext.Set<BusinessRecord>().SingleAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.DbContext.Set<DurableSendRecord>().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, Assert.Single(await fixture.DbContext.Set<DurableSendCapacityState>()
            .ToListAsync(TestContext.Current.CancellationToken)).StoredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "dispose-without-commit-fails-and-detaches")]
    public async Task Dispose_WithoutCommitFailsLoudlyAfterDetachingOnlyTheOutboxAsync()
    {
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        var business = new BusinessRecord(Guid.NewGuid(), "survives-disposal");
        fixture.DbContext.Add(business);
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), TestContext.Current.CancellationToken);

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(context.Dispose);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();

        Assert.Contains("disposed without commit", failure.Message, StringComparison.Ordinal);
        Assert.Equal(business, await fixture.DbContext.Set<BusinessRecord>().SingleAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.DbContext.Set<DurableSendRecord>().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, Assert.Single(await fixture.DbContext.Set<DurableSendCapacityState>()
            .ToListAsync(TestContext.Current.CancellationToken)).StoredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "externally-saved-session-is-signaled-once")]
    public async Task Commit_AfterExternalSaveRecognizesThePersistedSessionAndSignalsExactlyOnceAsync()
    {
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), TestContext.Current.CancellationToken);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await context.CommitAsync(TestContext.Current.CancellationToken);
        await context.CommitAsync(TestContext.Current.CancellationToken);

        Assert.Single(await fixture.DbContext.Set<DurableSendRecord>().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, Assert.Single(await fixture.DbContext.Set<DurableSendCapacityState>()
            .ToListAsync(TestContext.Current.CancellationToken)).StoredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "externally-saved-session-survives-context-disposal-order")]
    public async Task Dispose_AfterExternalSaveDoesNotAccessAnAlreadyDisposedDbContextAsync()
    {
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), TestContext.Current.CancellationToken);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await fixture.DbContext.DisposeAsync();

        Exception? failure = Record.Exception(context.Dispose);

        Assert.Null(failure);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-BUS-OUTBOX-WRITE", "concurrent-writes-share-one-state")]
    public async Task ConcurrentWrites_CreateOneStateAndRetainEveryDistinctMessageAsync()
    {
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Guid[] messageIds = Enumerable.Range(0, 32).Select(_ => Guid.NewGuid()).ToArray();
        Task[] writes = messageIds.Select(async (messageId, index) =>
        {
            await start.Task;
            await context.AddSendAsync(CreateSendContext(messageId, index));
        }).ToArray();

        start.SetResult();
        await Task.WhenAll(writes);

        DurableSendRecord[] messages = fixture.DbContext.Set<DurableSendRecord>().Local.ToArray();
        Assert.Equal(messageIds.Length, messages.Length);
        Assert.Equal(messageIds.Order(), messages.Select(message => message.Id).Order());
        Assert.All(messages, message => Assert.Equal("default", message.StoreKey));

        await context.AbortAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-BUS-OUTBOX-WRITE", "committed-batch-rolls-to-new-state")]
    public async Task CommittedBatch_StartsANewStateAndNotifiesExactlyOncePerBatchAsync()
    {
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), TestContext.Current.CancellationToken);
        await context.CommitAsync(TestContext.Current.CancellationToken);
        Guid firstOutboxId = Assert.Single(fixture.DbContext.Set<DurableSendRecord>().Local).Id;

        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 2), TestContext.Current.CancellationToken);
        DurableSendRecord[] states = fixture.DbContext.Set<DurableSendRecord>().Local.ToArray();

        Assert.Equal(2, states.Length);
        Assert.Single(states, state => state.Id == firstOutboxId);
        Assert.Single(states, state => state.Id != firstOutboxId);

        await context.CommitAsync(TestContext.Current.CancellationToken);
        context.Dispose();
        Assert.Equal(2, (await fixture.DbContext.Set<DurableSendRecord>()
            .ToListAsync(TestContext.Current.CancellationToken)).Count);
    }

    private static MessageSendContext<OutboxProbe> CreateSendContext(Guid messageId, int sequence) => new(
        new OutboxProbe(sequence))
    {
        MessageId = messageId,
        Serializer = ServiceBusMetadataJson.MessageSerializer,
        DestinationAddress = new Uri("loopback://transactional-outbox/probe"),
    };

    private sealed class MessageIdChangingSerializer(Guid replacementId) : IBoundedMessageSerializer
    {
        public ContentType ContentType { get; } = new("application/octet-stream");

        public SerializedTransportTextFormat TransportTextFormat => SerializedTransportTextFormat.Base64;

        public MessageBody GetMessageBody<T>(SendContext<T> context) where T : class =>
            throw new NotSupportedException("The bounded serializer path must be used.");

        public void WriteSerializedBody<T>(SendContext<T> context, IBufferWriter<byte> writer) where T : class
        {
            writer.GetSpan(1)[0] = 42;
            writer.Advance(1);
        }

        public void WriteTransportEnvelope<T>(SendContext<T> context, Stream serializedBody, IBufferWriter<byte> writer)
            where T : class
        {
            context.MessageId = replacementId;
            int value = serializedBody.ReadByte();
            if (value < 0)
                throw new InvalidOperationException("The admitted application body was empty.");
            writer.GetSpan(1)[0] = (byte)value;
            writer.Advance(1);
        }

        public bool TryLocateSerializedBody(ReadOnlySpan<byte> serializedEnvelope, out int offset, out int length)
        {
            offset = 0;
            length = serializedEnvelope.Length;
            return length == 1;
        }
    }

    public sealed record OutboxProbe(int Sequence);

    public sealed record BusinessRecord(Guid Id, string Value);

    public sealed class OutboxDbContext(DbContextOptions<OutboxDbContext> options) : DbContext(options)
    {
        public DbSet<BusinessRecord> BusinessRecords => Set<BusinessRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddViciOneReliableMessaging();
            modelBuilder.Entity<BusinessRecord>().HasKey(x => x.Id);
        }
    }

    private sealed class OutboxFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _services;

        private OutboxFixture(
            SqliteConnection connection,
            OutboxDbContext dbContext,
            IBus bus,
            RecordingNotification notification,
            ServiceProvider services)
        {
            _connection = connection;
            DbContext = dbContext;
            Bus = bus;
            Notification = notification;
            _services = services;
        }

        public IBus Bus { get; }

        public OutboxDbContext DbContext { get; }

        public RecordingNotification Notification { get; }

        public static async Task<OutboxFixture> CreateAsync(int maximumEnvelopeBytes = 2 * 1024 * 1024,
            SaveChangesInterceptor? interceptor = null)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var optionsBuilder = new DbContextOptionsBuilder<OutboxDbContext>().UseSqlite(connection);
            if (interceptor is not null)
                optionsBuilder.AddInterceptors(interceptor);
            var dbContext = new OutboxDbContext(optionsBuilder.Options);
            await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            var notification = new RecordingNotification();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddViciOneMessageContracts(catalog =>
                catalog.Register<OutboxProbe>("vicione.tests.transactional-outbox"));
            serviceCollection.AddViciOnePayloadAdmission<IBus>(options =>
            {
                options.MaximumSerializedBodyBytes = Math.Min(1024 * 1024, maximumEnvelopeBytes);
                options.MaximumTransportEnvelopeBytes = maximumEnvelopeBytes;
            });
            serviceCollection.AddViciOneReliableMessaging<IBus>(options =>
            {
                options.MaximumStoredCount = 100;
                options.MaximumStoredBytes = 1024 * 1024;
            });
            ServiceProvider services = serviceCollection.BuildServiceProvider();
            IBus bus = global::ViciOne.ServiceBus.Advanced.Bus.Factory.CreateUsingInMemory(_ => { });

            return new OutboxFixture(connection, dbContext, bus, notification, services);
        }

        public EntityFrameworkScopedBusContext<IBus, OutboxDbContext> CreateBusContext() => new(
            Bus,
            DbContext,
            Notification,
            DispatchProxy.Create<IClientFactory, ThrowingClientFactoryProxy>(),
            _services,
            new FakeTimeProvider(Now),
            BusPersistenceIdentity<IBus>.Create("default"));

        public OutboxDbContext CreateFreshContext() => new(
            new DbContextOptionsBuilder<OutboxDbContext>().UseSqlite(_connection).Options);

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _connection.DisposeAsync();
            await _services.DisposeAsync();
        }
    }

    private sealed class SuppressingSaveInterceptor : SaveChangesInterceptor
    {
        public bool Suppress { get; set; }
        public int ReportedSavedCount { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Suppress ? InterceptionResult<int>.SuppressWithResult(ReportedSavedCount) : result);
    }

    public class ThrowingClientFactoryProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException("The outbox write tests must not resolve a request client.");
    }

    public sealed class RecordingNotification : IBusOutboxNotification<EntityFrameworkBusOutboxScope<IBus, OutboxDbContext>>
    {
        private int _deliveredCount;

        public int DeliveredCount => Volatile.Read(ref _deliveredCount);

        public void SignalDelivery() => Interlocked.Increment(ref _deliveredCount);

        public Task WaitForDeliveryAsync(CancellationToken cancellationToken) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new InvalidOperationException("The outbox write tests must not wait for delivery."); }
    }
}
