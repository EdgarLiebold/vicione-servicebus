using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.DurableSend;

public sealed class EntityFrameworkReliableStoreTests
{
    private static readonly DateTimeOffset Epoch = DateTimeOffset.Parse("2026-09-03T12:00:00+00:00");

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DURABLE-MODEL", "bounded-record-capacity-and-claim-indexes")]
    public void Model_MapsBoundedRecordsCapacityLedgerAndClaimIndexes()
    {
        var options = new DbContextOptionsBuilder<DurableDbContext>()
            .UseSqlServer("Server=localhost;Database=not-opened;User Id=unused;Password=unused;Encrypt=False")
            .Options;
        using var context = new DurableDbContext(options, schema: "reliability");
        IEntityType record = context.Model.FindEntityType(typeof(DurableSendRecord))!;
        IEntityType capacity = context.Model.FindEntityType(typeof(DurableSendCapacityState))!;

        Assert.Equal("vicione_outbox", record.GetTableName());
        Assert.Equal("reliability", record.GetSchema());
        Assert.Equal([nameof(DurableSendRecord.StoreKey), nameof(DurableSendRecord.Id)],
            record.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Equal(BusPersistenceIdentity<ITestBus>.MaximumLength,
            record.FindProperty(nameof(DurableSendRecord.StoreKey))!.GetMaxLength());
        Assert.Equal(128, BusPersistenceIdentity<ITestBus>.MaximumLength);
        Assert.Equal(320, record.FindProperty(nameof(DurableSendRecord.ContractIdentity))!.GetMaxLength());
        Assert.Equal(SerializedDurableSend.MaximumDestinationAddressCharacters,
            record.FindProperty(nameof(DurableSendRecord.DestinationAddress))!.GetMaxLength());
        Assert.Equal(SerializedDurableSend.MaximumContentTypeCharacters,
            record.FindProperty(nameof(DurableSendRecord.ContentType))!.GetMaxLength());
        Assert.Equal(512, record.FindProperty(nameof(DurableSendRecord.LastFailureType))!.GetMaxLength());
        Assert.Contains(record.GetIndexes(), index => index.Properties.Select(property => property.Name)
            .SequenceEqual([
                nameof(DurableSendRecord.StoreKey),
                nameof(DurableSendRecord.Status),
                nameof(DurableSendRecord.NextAttemptAt),
                nameof(DurableSendRecord.EnqueuedAt),
            ]));
        Assert.Contains(record.GetIndexes(), index => index.Properties.Select(property => property.Name)
            .SequenceEqual([nameof(DurableSendRecord.StoreKey), nameof(DurableSendRecord.LeaseExpiresAt)]));
        Assert.Equal("vicione_reliable_capacity", capacity.GetTableName());
        Assert.Equal(nameof(DurableSendCapacityState.StoreKey), Assert.Single(capacity.FindPrimaryKey()!.Properties).Name);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DURABLE-STORE", "real-sqlite-lifecycle-and-generation-fencing")]
    public async Task Store_PersistsTheCompleteLifecycleAndFencesAReusedIdentityGenerationAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DurableDatabase database = await DurableDatabase.CreateAsync(cancellationToken);
        var validator = new RecordingValidator();
        IOutboxStore<ITestBus> store = database.CreateStore<ITestBus>("lifecycle", validator);
        SerializedDurableSend message = Message(1, body: [1, 2, 3], metadata: [4, 5]);
        var limits = new DurableSendStoreLimits(1, 5);

        DurableSendAdmissionResult accepted = await store.AdmitAsync(message, limits, Epoch, cancellationToken);
        DurableSendAdmissionResult duplicate = await store.AdmitAsync(message, limits, Epoch.AddHours(1), cancellationToken);
        Assert.Equal(DurableSendAdmissionDisposition.Accepted, accepted.Disposition);
        Assert.Equal(DurableSendAdmissionDisposition.AlreadyAccepted, duplicate.Disposition);
        Assert.Equal((1, 5L), (duplicate.StoredCount, duplicate.StoredBytes));
        await Assert.ThrowsAsync<DurableSendIdentityConflictException>(() =>
            store.AdmitAsync(message with { Metadata = new byte[] { 9, 9 } }, limits, Epoch, cancellationToken));
        await Assert.ThrowsAsync<DurableSendIdentityConflictException>(() =>
            store.AdmitAsync(message with { DueAt = Epoch.AddMinutes(1) }, limits, Epoch, cancellationToken));
        await Assert.ThrowsAsync<DurableSendIdentityConflictException>(() =>
            store.AdmitAsync(message with
            {
                ContractIdentity = new MessageContractIdentity("vicione.tests.ef-durable-alternate", 1),
            }, limits, Epoch, cancellationToken));
        await Assert.ThrowsAsync<DurableSendIdentityConflictException>(() =>
            store.AdmitAsync(message with { DestinationAddress = new Uri("loopback://ef-durable/other") },
                limits, Epoch, cancellationToken));
        await Assert.ThrowsAsync<DurableSendIdentityConflictException>(() =>
            store.AdmitAsync(message with { ContentType = "application/json" }, limits, Epoch, cancellationToken));
        await Assert.ThrowsAsync<DurableSendIdentityConflictException>(() =>
            store.AdmitAsync(message with { ContentType = "APPLICATION/octet-stream" },
                limits, Epoch, cancellationToken));
        await Assert.ThrowsAsync<DurableSendIdentityConflictException>(() =>
            store.AdmitAsync(message with { MessageId = GuidFrom(81) }, limits, Epoch, cancellationToken));
        await Assert.ThrowsAsync<DurableSendIdentityConflictException>(() =>
            store.AdmitAsync(message with { CorrelationId = GuidFrom(82) }, limits, Epoch, cancellationToken));
        await Assert.ThrowsAsync<DurableSendIdentityConflictException>(() =>
            store.AdmitAsync(message with { Body = new byte[] { 9, 2, 3 } }, limits, Epoch, cancellationToken));
        await Assert.ThrowsAsync<DurableSendCapacityExceededException>(() =>
            store.AdmitAsync(Message(2, body: [], metadata: []), limits, Epoch, cancellationToken));

        DurableSendDelivery first = Assert.Single(await store.ClaimDueAsync(
            Epoch,
            1,
            TimeSpan.FromMinutes(1),
            cancellationToken));
        Assert.Equal(message.ContractIdentity, first.Message.ContractIdentity);
        Assert.Equal(message.DestinationAddress, first.Message.DestinationAddress);
        Assert.Equal(message.ContentType, first.Message.ContentType);
        Assert.Equal(message.MessageId, first.Message.MessageId);
        Assert.Equal(message.CorrelationId, first.Message.CorrelationId);
        Assert.Equal(message.DueAt, first.Message.DueAt);
        Assert.Equal(message.Body.ToArray(), first.Message.Body.ToArray());
        Assert.Equal(message.Metadata.ToArray(), first.Message.Metadata.ToArray());
        Assert.True(await store.ScheduleRetryAsync(
            message.Id,
            first.Lease,
            1,
            Epoch.AddMinutes(2),
            DurableSendFailureKind.Transient,
            "Tests.Transient",
            Epoch,
            cancellationToken));
        Assert.Empty(await store.ClaimDueAsync(
            Epoch.AddMinutes(2).AddTicks(-1),
            1,
            TimeSpan.FromMinutes(1),
            cancellationToken));

        DurableSendDelivery retry = Assert.Single(await store.ClaimDueAsync(
            Epoch.AddMinutes(2),
            1,
            TimeSpan.FromMinutes(1),
            cancellationToken));
        Assert.Equal(1, retry.DeliveryAttempts);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.QuarantineAsync(
            message.Id,
            first.Lease,
            2,
            DurableSendFailureKind.NonRetryable,
            "Tests.StaleLease",
            Epoch.AddMinutes(2),
            cancellationToken));
        Assert.True(await store.AwaitConsumerCompletionAsync(
            message.Id,
            retry.Lease,
            2,
            Epoch.AddMinutes(7),
            cancellationToken));
        Assert.Equal(1, (await store.GetSnapshotAsync(cancellationToken)).AwaitingConsumerCompletionCount);
        Assert.True(await store.CompleteConsumerDeliveryAsync(
            message.Id,
            retry.GenerationToken,
            Epoch.AddMinutes(3),
            cancellationToken));
        Assert.Equal(0, (await store.GetSnapshotAsync(cancellationToken)).StoredCount);

        await store.AdmitAsync(message, limits, Epoch.AddMinutes(4), cancellationToken);
        DurableSendDelivery incarnation = Assert.Single(await store.ClaimDueAsync(
            Epoch.AddMinutes(4),
            1,
            TimeSpan.FromMinutes(1),
            cancellationToken));
        Assert.NotEqual(retry.GenerationToken, incarnation.GenerationToken);
        Assert.False(await store.CompleteConsumerDeliveryAsync(
            message.Id,
            retry.GenerationToken,
            Epoch.AddMinutes(4),
            cancellationToken));
        Assert.True(await store.QuarantineAsync(
            message.Id,
            incarnation.Lease,
            1,
            DurableSendFailureKind.NonRetryable,
            "Tests.Permanent",
            Epoch.AddMinutes(4),
            cancellationToken));
        DurableSendQuarantinePage page = await store.GetQuarantineAsync(
            DurableSendQuarantineQuery.FirstPage(1),
            cancellationToken);
        DurableSendQuarantineEntry evidence = Assert.Single(page.Entries);
        Assert.Equal(message.Id, evidence.Id);
        Assert.Equal(DurableSendFailureKind.NonRetryable, evidence.FailureKind);
        Assert.Equal(DurableSendAdmissionDisposition.AlreadyQuarantined,
            (await store.AdmitAsync(message, limits, Epoch, cancellationToken)).Disposition);

        DurableSendOperationResult requeue = await store.RequeueAsync(
            message.Id,
            Epoch.AddMinutes(5),
            cancellationToken);
        Assert.Equal(DurableSendOperationOutcome.Requeued, requeue.Outcome);
        DurableSendDelivery requeued = Assert.Single(await store.ClaimDueAsync(
            Epoch.AddMinutes(5),
            1,
            TimeSpan.FromMinutes(1),
            cancellationToken));
        Assert.Equal(0, requeued.DeliveryAttempts);
        Assert.True(await store.MarkDeliveredAsync(message.Id, requeued.Lease, Epoch.AddMinutes(5), cancellationToken));
        DurableSendStoreSnapshot empty = await store.GetSnapshotAsync(cancellationToken);
        Assert.Equal((0, 0L, 0, 0),
            (empty.StoredCount, empty.StoredBytes, empty.PendingCount, empty.QuarantinedCount));
        Assert.Equal(1, validator.CallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-RELIABLE-RECOVERY", "cancellation-boundary-restart-and-immutable-schedule-identity")]
    public async Task Store_CommittedScheduleSurvivesCancellationRestartAndRetryWithoutChangingItsIdentityAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DurableDatabase database = await DurableDatabase.CreateAsync(cancellationToken);
        IOutboxStore<ITestBus> firstProcess = database.CreateStore<ITestBus>("restart", new RecordingValidator());
        DateTimeOffset dueAt = Epoch.AddMinutes(10);
        SerializedDurableSend scheduled = Message(41) with { DueAt = dueAt };
        var limits = new DurableSendStoreLimits(10, 100);
        using var cancelledAfterCommit = new CancellationTokenSource();

        DurableSendAdmissionResult committed = await firstProcess.AdmitAsync(
            scheduled,
            limits,
            Epoch,
            cancellationToken);
        cancelledAfterCommit.Cancel();
        Assert.True(committed.IsNew);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => firstProcess.GetSnapshotAsync(cancelledAfterCommit.Token));

        IOutboxStore<ITestBus> restarted = database.CreateStore<ITestBus>("restart", new RecordingValidator());
        Assert.Empty(await restarted.ClaimDueAsync(
            dueAt.AddTicks(-1),
            1,
            TimeSpan.FromMinutes(1),
            cancellationToken));
        DurableSendDelivery firstAttempt = Assert.Single(await restarted.ClaimDueAsync(
            dueAt,
            1,
            TimeSpan.FromMinutes(1),
            cancellationToken));
        Assert.Equal(dueAt, firstAttempt.Message.DueAt);
        ReliableMessagingOperationResult claimedCancellation = await ((IScheduleStore<ITestBus>)restarted).CancelAsync(
            scheduled.Id,
            cancellationToken);
        Assert.Equal(ReliableMessagingOperationDisposition.InvalidState, claimedCancellation.Disposition);
        Assert.Equal(1, (await restarted.GetSnapshotAsync(cancellationToken)).StoredCount);
        Assert.True(await restarted.ScheduleRetryAsync(
            scheduled.Id,
            firstAttempt.Lease,
            1,
            dueAt.AddMinutes(5),
            DurableSendFailureKind.Transient,
            "Tests.BrokerOffline",
            dueAt,
            cancellationToken));

        DurableSendAdmissionResult duplicate = await restarted.AdmitAsync(
            scheduled with { DueAt = dueAt.ToOffset(TimeSpan.FromHours(5)) },
            limits,
            Epoch,
            cancellationToken);
        Assert.Equal(DurableSendAdmissionDisposition.AlreadyAccepted, duplicate.Disposition);
        DurableSendDelivery retry = Assert.Single(await restarted.ClaimDueAsync(
            dueAt.AddMinutes(5),
            1,
            TimeSpan.FromMinutes(1),
            cancellationToken));
        Assert.Equal(dueAt, retry.Message.DueAt);
        Assert.Equal(1, retry.DeliveryAttempts);
        Assert.True(await restarted.MarkDeliveredAsync(
            scheduled.Id,
            retry.Lease,
            dueAt.AddMinutes(5),
            cancellationToken));
        Assert.Equal(0, (await restarted.GetSnapshotAsync(cancellationToken)).StoredCount);

        IOutboxStore<ITestBus> cancellationStore = database.CreateStore<ITestBus>("pre-cancel", new RecordingValidator());
        using var cancelledBeforeCommit = new CancellationTokenSource();
        cancelledBeforeCommit.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancellationStore.AdmitAsync(
            Message(42),
            limits,
            Epoch,
            cancelledBeforeCommit.Token));
        Assert.Equal(0, (await cancellationStore.GetSnapshotAsync(cancellationToken)).StoredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-RELIABLE-INBOX", "restart-stable-duplicate-retry-quarantine-and-abandon")]
    public async Task Inbox_PersistsDuplicateRetryQuarantineAndAbandonAcrossStoreRestartsAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DurableDatabase database = await DurableDatabase.CreateAsync(cancellationToken);
        IInboxStore<ITestBus> firstProcess = Assert.IsAssignableFrom<IInboxStore<ITestBus>>(
            database.CreateStore<ITestBus>("inbox-restart", new RecordingValidator()));
        var consumedKey = new ReliableInboxKey(GuidFrom(51), GuidFrom(52));
        ReliableInboxAcquireResult first = await firstProcess.AcquireAsync(
            consumedKey,
            Epoch,
            TimeSpan.FromMinutes(1),
            cancellationToken);
        Assert.True(await firstProcess.CompleteAsync(
            consumedKey,
            Assert.IsType<ReliableInboxLease>(first.Lease),
            Epoch,
            cancellationToken));

        IInboxStore<ITestBus> restarted = Assert.IsAssignableFrom<IInboxStore<ITestBus>>(
            database.CreateStore<ITestBus>("inbox-restart", new RecordingValidator()));
        Assert.Equal(
            ReliableInboxAcquireDisposition.AlreadyConsumed,
            (await restarted.AcquireAsync(
                consumedKey,
                Epoch.AddYears(1),
                TimeSpan.FromMinutes(1),
                cancellationToken)).Disposition);

        var retryKey = new ReliableInboxKey(GuidFrom(53), GuidFrom(54));
        ReliableInboxAcquireResult acquired = await restarted.AcquireAsync(
            retryKey,
            Epoch,
            TimeSpan.FromMinutes(1),
            cancellationToken);
        DateTimeOffset retryAt = Epoch.AddMinutes(5);
        Assert.True(await restarted.ScheduleRetryAsync(
            retryKey,
            Assert.IsType<ReliableInboxLease>(acquired.Lease),
            retryAt,
            "Tests.Transient",
            Epoch,
            cancellationToken));

        IInboxStore<ITestBus> secondRestart = Assert.IsAssignableFrom<IInboxStore<ITestBus>>(
            database.CreateStore<ITestBus>("inbox-restart", new RecordingValidator()));
        Assert.Equal(
            ReliableInboxAcquireDisposition.NotDue,
            (await secondRestart.AcquireAsync(
                retryKey,
                retryAt.AddTicks(-1),
                TimeSpan.FromMinutes(1),
                cancellationToken)).Disposition);
        ReliableInboxAcquireResult retry = await secondRestart.AcquireAsync(
            retryKey,
            retryAt,
            TimeSpan.FromMinutes(1),
            cancellationToken);
        Assert.Equal(2, retry.Attempt);
        Assert.True(await secondRestart.QuarantineAsync(
            retryKey,
            Assert.IsType<ReliableInboxLease>(retry.Lease),
            "Tests.Permanent",
            retryAt,
            cancellationToken));
        ReliableInboxQuarantineEntry entry = Assert.Single((await secondRestart.GetQuarantineAsync(
            new ReliableInboxQuarantineQuery { PageSize = 1 },
            cancellationToken)).Entries);
        Assert.Equal(retryKey, entry.Key);
        Assert.Equal(2, entry.Attempts);
        DateTimeOffset abandonedAt = retryAt.AddMinutes(1).ToOffset(TimeSpan.FromHours(5));
        Assert.Equal(
            ReliableMessagingOperationDisposition.Applied,
            (await secondRestart.AbandonAsync(retryKey, abandonedAt, cancellationToken)).Disposition);
        Assert.Empty((await secondRestart.GetQuarantineAsync(
            new ReliableInboxQuarantineQuery { PageSize = 1 },
            cancellationToken)).Entries);
        await using (DurableDbContext persisted = database.Factory.CreateDbContext())
        {
            ReliableInboxRecord abandoned = await persisted.Set<ReliableInboxRecord>().AsNoTracking().SingleAsync(
                row => row.StoreKey == "inbox-restart"
                    && row.MessageId == retryKey.MessageId
                    && row.ConsumerId == retryKey.ConsumerId,
                cancellationToken);
            Assert.Equal(ReliableInboxStatus.Abandoned, abandoned.Status);
            Assert.Equal(abandonedAt.UtcDateTime, abandoned.CompletedAt);
        }
        Assert.Equal(
            ReliableInboxAcquireDisposition.Unavailable,
            (await secondRestart.AcquireAsync(
                retryKey,
                retryAt.AddYears(1),
                TimeSpan.FromMinutes(1),
                cancellationToken)).Disposition);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-RELIABLE-INBOX", "concurrent-first-acquisition-has-one-owner")]
    public async Task Inbox_ConcurrentFirstAcquisitionProducesExactlyOneLeaseAsync()
    {
        const int contenderCount = 32;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DurableDatabase database = await DurableDatabase.CreateAsync(cancellationToken);
        IInboxStore<ITestBus> store = Assert.IsAssignableFrom<IInboxStore<ITestBus>>(
            database.CreateStore<ITestBus>("inbox-concurrent", new RecordingValidator()));
        var key = new ReliableInboxKey(GuidFrom(55), GuidFrom(56));
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<ReliableInboxAcquireResult>[] contenders = Enumerable.Range(0, contenderCount)
            .Select(async _ =>
            {
                await start.Task.WaitAsync(cancellationToken);
                return await store.AcquireAsync(key, Epoch, TimeSpan.FromMinutes(1), cancellationToken);
            })
            .ToArray();

        start.SetResult();
        ReliableInboxAcquireResult[] results = await Task.WhenAll(contenders);

        ReliableInboxAcquireResult acquired = Assert.Single(
            results,
            result => result.Disposition == ReliableInboxAcquireDisposition.Acquired);
        Assert.NotNull(acquired.Lease);
        Assert.Equal(1, acquired.Attempt);
        Assert.Equal(
            contenderCount - 1,
            results.Count(result => result.Disposition == ReliableInboxAcquireDisposition.Busy));
        Assert.All(results, result => Assert.Equal(1, result.Attempt));

        await using DurableDbContext persisted = database.Factory.CreateDbContext();
        ReliableInboxRecord row = await persisted.Set<ReliableInboxRecord>().AsNoTracking().SingleAsync(
            candidate => candidate.StoreKey == "inbox-concurrent"
                && candidate.MessageId == key.MessageId
                && candidate.ConsumerId == key.ConsumerId,
            cancellationToken);
        Assert.Equal(ReliableInboxStatus.Processing, row.Status);
        Assert.Equal(1, row.Attempts);
        Assert.Equal(acquired.Lease!.Value.Token, row.LeaseToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-RELIABLE-INBOX", "concurrent-operator-decisions-have-one-winner")]
    public async Task Inbox_ConcurrentOperatorDecisionsApplyExactlyOnceAsync()
    {
        const int contenderCount = 24;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DurableDatabase database = await DurableDatabase.CreateAsync(cancellationToken);
        IInboxStore<ITestBus> store = Assert.IsAssignableFrom<IInboxStore<ITestBus>>(
            database.CreateStore<ITestBus>("inbox-operator-race", new RecordingValidator()));
        var key = new ReliableInboxKey(GuidFrom(57), GuidFrom(58));
        ReliableInboxAcquireResult acquired = await store.AcquireAsync(
            key,
            Epoch,
            TimeSpan.FromMinutes(1),
            cancellationToken);
        Assert.True(await store.QuarantineAsync(
            key,
            Assert.IsType<ReliableInboxLease>(acquired.Lease),
            "Tests.Permanent",
            Epoch,
            cancellationToken));
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<ReliableMessagingOperationResult>[] contenders = Enumerable.Range(0, contenderCount)
            .Select(async index =>
            {
                await start.Task.WaitAsync(cancellationToken);
                return (index % 3) switch
                {
                    0 => await store.RequeueAsync(key, Epoch.AddMinutes(1), cancellationToken),
                    1 => await store.DiscardAsync(key, cancellationToken),
                    _ => await store.AbandonAsync(key, Epoch.AddMinutes(2), cancellationToken),
                };
            })
            .ToArray();

        start.SetResult();
        ReliableMessagingOperationResult[] results = await Task.WhenAll(contenders);

        ReliableMessagingOperationResult applied = Assert.Single(
            results,
            result => result.Disposition == ReliableMessagingOperationDisposition.Applied);
        Assert.All(
            results.Where(result => !ReferenceEquals(result, applied)),
            result => Assert.Contains(
                result.Disposition,
                new[]
                {
                    ReliableMessagingOperationDisposition.InvalidState,
                    ReliableMessagingOperationDisposition.NotFound,
                }));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DURABLE-CAPACITY", "concurrent-conditional-ledger-admission")]
    public async Task Store_ConcurrentAdmissionsCannotOvershootTheHardLedgerAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DurableDatabase database = await DurableDatabase.CreateAsync(cancellationToken);
        IOutboxStore<ITestBus> store = database.CreateStore<ITestBus>("concurrent", new RecordingValidator());
        var limits = new DurableSendStoreLimits(5, 20);

        Task<bool>[] attempts = Enumerable.Range(1, 20).Select(async id =>
        {
            try
            {
                await store.AdmitAsync(Message(id), limits, Epoch, cancellationToken);
                return true;
            }
            catch (DurableSendCapacityExceededException)
            {
                return false;
            }
        }).ToArray();

        bool[] results = await Task.WhenAll(attempts);
        DurableSendStoreSnapshot snapshot = await store.GetSnapshotAsync(cancellationToken);
        Assert.Equal(5, results.Count(static accepted => accepted));
        Assert.Equal((5, 5L), (snapshot.StoredCount, snapshot.StoredBytes));

        IOutboxStore<ITestBus> byteStore = database.CreateStore<ITestBus>(
            "concurrent-bytes",
            new RecordingValidator());
        var byteLimits = new DurableSendStoreLimits(20, 5);
        Task<bool>[] byteAttempts = Enumerable.Range(101, 20).Select(async id =>
        {
            try
            {
                await byteStore.AdmitAsync(Message(id), byteLimits, Epoch, cancellationToken);
                return true;
            }
            catch (DurableSendCapacityExceededException)
            {
                return false;
            }
        }).ToArray();

        bool[] byteResults = await Task.WhenAll(byteAttempts);
        DurableSendStoreSnapshot byteSnapshot = await byteStore.GetSnapshotAsync(cancellationToken);
        Assert.Equal(5, byteResults.Count(static accepted => accepted));
        Assert.Equal((5, 5L), (byteSnapshot.StoredCount, byteSnapshot.StoredBytes));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DURABLE-RECOVERY", "missing-ledger-rebuilt-by-server-aggregate")]
    public async Task Store_ReconstructsAMissingCapacityLedgerFromRetainedRowsAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DurableDatabase database = await DurableDatabase.CreateAsync(cancellationToken);
        await using (DurableDbContext seed = database.Factory.CreateDbContext())
        {
            seed.AddRange(
                Record("recovery", 1, storageSize: 3),
                Record("recovery", 2, storageSize: 7));
            await seed.SaveChangesAsync(cancellationToken);
        }
        IOutboxStore<ITestBus> store = database.CreateStore<ITestBus>("recovery", new RecordingValidator());

        DurableSendStoreSnapshot snapshot = await store.GetSnapshotAsync(cancellationToken);

        Assert.Equal((2, 10L, 2), (snapshot.StoredCount, snapshot.StoredBytes, snapshot.PendingCount));
        await using DurableDbContext verify = database.Factory.CreateDbContext();
        DurableSendCapacityState ledger = await verify.Set<DurableSendCapacityState>()
            .SingleAsync(item => item.StoreKey == "recovery", cancellationToken);
        Assert.Equal((2, 10L), (ledger.StoredCount, ledger.StoredBytes));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DURABLE-PAGINATION", "restart-stable-complete-seek-traversal-with-concurrent-changes")]
    public async Task QuarantinePagination_ResumesAfterRestartAndTraversesEveryEqualTimestampExactlyOnceAsync()
    {
        const int retainedCount = 1005;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DurableDatabase database = await DurableDatabase.CreateAsync(cancellationToken);
        DurableSendId[] expected = Enumerable.Range(1, retainedCount)
            .Select(index => new DurableSendId(PageGuid(index)))
            .ToArray();
        await using (DurableDbContext seed = database.Factory.CreateDbContext())
        {
            seed.AddRange(expected.Select(id => QuarantinedRecord("paged", id, Epoch)));
            seed.Add(QuarantinedRecord("another-bus", new DurableSendId(PageGuid(retainedCount + 10)), Epoch));
            await seed.SaveChangesAsync(cancellationToken);
        }

        IOutboxStore<ITestBus> firstProcess = database.CreateStore<ITestBus>("paged", new RecordingValidator());
        DurableSendQuarantinePage page = await firstProcess.GetQuarantineAsync(
            DurableSendQuarantineQuery.FirstPage(128),
            cancellationToken);
        var actual = page.Entries.Select(entry => entry.Id).ToList();
        Assert.True(page.HasMore);

        Assert.Equal(DurableSendOperationOutcome.Requeued,
            (await firstProcess.RequeueAsync(actual[0], Epoch.AddMinutes(1), cancellationToken)).Outcome);
        Assert.Equal(DurableSendOperationOutcome.Discarded,
            (await firstProcess.DiscardQuarantinedAsync(actual[1], cancellationToken)).Outcome);
        await using (DurableDbContext concurrent = database.Factory.CreateDbContext())
        {
            concurrent.Add(QuarantinedRecord(
                "paged",
                new DurableSendId(PageGuid(retainedCount + 1)),
                Epoch.AddMinutes(1)));
            await concurrent.SaveChangesAsync(cancellationToken);
        }

        // A new store instance models a process restart; the opaque token contains all cursor state.
        IOutboxStore<ITestBus> restarted = database.CreateStore<ITestBus>("paged", new RecordingValidator());
        while (page.NextQuery is { } next)
        {
            page = await restarted.GetQuarantineAsync(next, cancellationToken);
            actual.AddRange(page.Entries.Select(entry => entry.Id));
        }

        Assert.Equal(expected, actual);
        Assert.Equal(retainedCount, actual.Distinct().Count());
        Assert.Equal(new DurableSendId(PageGuid(retainedCount + 1)),
            Assert.Single((await restarted.GetQuarantineAsync(
                DurableSendQuarantineQuery.FirstPage(1),
                cancellationToken)).Entries).Id);
        Assert.Equal(DurableSendOperationOutcome.NotQuarantined,
            (await restarted.RequeueAsync(expected[0], Epoch, cancellationToken)).Outcome);
        Assert.Equal(DurableSendOperationOutcome.NotFound,
            (await restarted.DiscardQuarantinedAsync(new DurableSendId(Guid.NewGuid()), cancellationToken)).Outcome);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DURABLE-PREFLIGHT", "custom-validator-runs-before-store-query")]
    public async Task Store_ExecutesTheProviderDurabilityPreflightBeforeInitializationQueriesAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expected = new ExpectedPreflightException();
        var validator = new ThrowingValidator(expected);
        var factory = new CountingFactory(new DbContextOptionsBuilder<DurableDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options);
        var store = new EntityFrameworkReliableStore<ITestBus, DurableDbContext>(
            factory,
            BusPersistenceIdentity<ITestBus>.Create("preflight"),
            validator);

        ExpectedPreflightException actual = await Assert.ThrowsAsync<ExpectedPreflightException>(() =>
            store.GetSnapshotAsync(cancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(1, validator.CallCount);
        Assert.Equal(1, factory.CreateCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DURABLE-COMPOSITION", "stable-persistence-identity-custom-validator-and-single-owner")]
    public void Registration_PreservesProviderValidatorAndUsesOneValidatedPersistenceIdentity()
    {
        var validator = new RecordingValidator();
        var services = new ServiceCollection();
        services.AddSingleton<IEntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>>(validator);

        Assert.Same(services, services.AddEntityFrameworkReliableStore<ITestBus, DurableDbContext>());
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IEntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>)
            && ReferenceEquals(descriptor.ImplementationInstance, validator));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IOutboxStore<ITestBus>));
        Assert.Throws<ConfigurationException>(() =>
            services.AddEntityFrameworkReliableStore<ITestBus, DurableDbContext>());

        var factory = new CountingFactory(new DbContextOptionsBuilder<DurableDbContext>().Options);
        Assert.Throws<ArgumentException>(() => BusPersistenceIdentity<ITestBus>.Create(" "));
        string exactMaximumIdentity = new('x', BusPersistenceIdentity<ITestBus>.MaximumLength);
        Assert.Equal(exactMaximumIdentity,
            BusPersistenceIdentity<ITestBus>.Create(exactMaximumIdentity).Require("test"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BusPersistenceIdentity<ITestBus>.Create(new string('x', BusPersistenceIdentity<ITestBus>.MaximumLength + 1)));
        Assert.Throws<ArgumentException>(() => BusPersistenceIdentity<ITestBus>.Create("bad\nkey"));

        var defaultStore = new EntityFrameworkReliableStore<IBus, DurableDbContext>(
            factory,
            BusPersistenceIdentity<IBus>.Create("default"),
            new RecordingValidator<IBus>());
        Assert.NotNull(defaultStore);
    }

    private static SerializedDurableSend Message(int id, byte[]? body = null, byte[]? metadata = null) => new()
    {
        Id = new DurableSendId(GuidFrom(id)),
        ContractIdentity = new MessageContractIdentity("vicione.tests.ef-durable", 1),
        DestinationAddress = new Uri("loopback://ef-durable"),
        ContentType = "application/octet-stream",
        Body = body ?? new byte[] { 1 },
        Metadata = metadata ?? [],
    };

    private static DurableSendRecord Record(string storeKey, int id, long storageSize) => new()
    {
        StoreKey = storeKey,
        Id = GuidFrom(id),
        GenerationToken = Guid.NewGuid(),
        ContractIdentity = new MessageContractIdentity("vicione.tests.ef-recovery", 1).ToString(),
        DestinationAddress = "loopback://ef-recovery/",
        ContentType = "application/octet-stream",
        Body = new byte[checked((int)storageSize)],
        StorageSize = storageSize,
        Status = DurableSendStatus.Pending,
        EnqueuedAt = Epoch.UtcDateTime,
    };

    private static DurableSendRecord QuarantinedRecord(
        string storeKey,
        DurableSendId id,
        DateTimeOffset quarantinedAt) => new()
        {
            StoreKey = storeKey,
            Id = id.Value,
            GenerationToken = Guid.NewGuid(),
            ContractIdentity = new MessageContractIdentity("vicione.tests.ef-page", 1).ToString(),
            DestinationAddress = "loopback://ef-page/",
            ContentType = "application/octet-stream",
            Body = [1],
            StorageSize = 1,
            Status = DurableSendStatus.Quarantined,
            EnqueuedAt = Epoch.UtcDateTime,
            DeliveryAttempts = 1,
            LastFailureKind = DurableSendFailureKind.NonRetryable,
            LastFailureType = "Tests.Page",
            LastFailureAt = quarantinedAt.UtcDateTime,
            QuarantinedAt = quarantinedAt.UtcDateTime,
        };

    private static Guid GuidFrom(int value) => new(value, 0, 0, new byte[8]);

    private static Guid PageGuid(int value) =>
        Guid.Parse($"00000000-0000-0000-0000-{value:x12}");

    private interface ITestBus : IBus;

    private sealed class DurableDbContext(DbContextOptions<DurableDbContext> options, string? schema = null)
        : DbContext(options)
    {
        private readonly string? _schema = schema;

        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddViciOneReliableMessaging(_schema);
    }

    private sealed class DurableDbContextFactory(DbContextOptions<DurableDbContext> options)
        : IDbContextFactory<DurableDbContext>
    {
        public DurableDbContext CreateDbContext() => new(options);
    }

    private sealed class CountingFactory(DbContextOptions<DurableDbContext> options)
        : IDbContextFactory<DurableDbContext>
    {
        public int CreateCount { get; private set; }

        public DurableDbContext CreateDbContext()
        {
            CreateCount++;
            return new DurableDbContext(options);
        }
    }

    private class RecordingValidator<TBus> : IEntityFrameworkDurableSendCommitDurabilityValidator<TBus>
        where TBus : class, IBus
    {
        public int CallCount { get; private set; }

        public Task ValidateAsync(DbContext dbContext, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingValidator : RecordingValidator<ITestBus>;

    private sealed class ThrowingValidator(ExpectedPreflightException exception)
        : IEntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>
    {
        public int CallCount { get; private set; }

        public Task ValidateAsync(DbContext dbContext, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            return Task.FromException(exception);
        }
    }

    private sealed class DurableDatabase : IAsyncDisposable
    {
        private readonly string _path;

        private DurableDatabase(string path, DurableDbContextFactory factory)
        {
            _path = path;
            Factory = factory;
        }

        public DurableDbContextFactory Factory { get; }

        public static async Task<DurableDatabase> CreateAsync(CancellationToken cancellationToken)
        {
            string path = Path.Combine(Path.GetTempPath(), $"vicione-durable-store-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<DurableDbContext>()
                .UseSqlite($"Data Source={path};Default Timeout=30;Pooling=False")
                .Options;
            var factory = new DurableDbContextFactory(options);
            await using DurableDbContext context = factory.CreateDbContext();
            await context.Database.EnsureCreatedAsync(cancellationToken);
            return new DurableDatabase(path, factory);
        }

        public IOutboxStore<TBus> CreateStore<TBus>(
            string storeKey,
            IEntityFrameworkDurableSendCommitDurabilityValidator<TBus> validator)
            where TBus : class, IBus =>
            new EntityFrameworkReliableStore<TBus, DurableDbContext>(
                Factory,
                BusPersistenceIdentity<TBus>.Create(storeKey),
                validator);

        public ValueTask DisposeAsync()
        {
            File.Delete(_path);
            File.Delete(_path + "-wal");
            File.Delete(_path + "-shm");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ExpectedPreflightException : Exception;
}
