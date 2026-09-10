using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.DurableSend;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DurableSend;

public sealed class InMemoryReliableStoreTests
{
    private static readonly DateTimeOffset Epoch = DateTimeOffset.Parse("2026-09-03T12:00:00+00:00");

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-STORE-ADMISSION", "exact-idempotence-without-second-capacity-charge")]
    public async Task Admission_ExactDuplicateIsIdempotentWithoutASecondCapacityChargeAsync()
    {
        StoreHarness store = Store();
        SerializedDurableSend message = Message(body: [1, 2], metadata: [3]);
        var limits = new DurableSendStoreLimits(1, 3);

        DurableSendAdmissionResult first = await store.AdmitAsync(message, limits, Epoch);
        DurableSendAdmissionResult second = await store.AdmitAsync(message, limits, Epoch.AddMinutes(1));

        Assert.Equal(DurableSendAdmissionDisposition.Accepted, first.Disposition);
        Assert.Equal(DurableSendAdmissionDisposition.AlreadyAccepted, second.Disposition);
        Assert.True(first.IsNew);
        Assert.False(second.IsNew);
        Assert.Equal(message.Id, second.Id);
        Assert.Equal(1, second.StoredCount);
        Assert.Equal(3, second.StoredBytes);
        Assert.Equal(1, (await store.GetSnapshotAsync()).StoredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-STORE-ADMISSION", "same-id-different-intent-conflicts")]
    public async Task Admission_SameIdWithAnyDifferentImmutableIntentFailsLoudlyAsync()
    {
        StoreHarness store = Store();
        SerializedDurableSend original = Message(
            body: [1, 2],
            metadata: [3, 4],
            messageId: Guid.Parse("10000000-0000-0000-0000-000000000001"),
            correlationId: Guid.Parse("20000000-0000-0000-0000-000000000002"));
        await store.AdmitAsync(original, new DurableSendStoreLimits(20, 1000), Epoch);

        SerializedDurableSend[] conflicts =
        [
            original with { ContractIdentity = new MessageContractIdentity("vicione.tests.changed", 1) },
            original with { DestinationAddress = new Uri("loopback://different") },
            original with { ContentType = "application/json" },
            original with { MessageId = Guid.NewGuid() },
            original with { CorrelationId = Guid.NewGuid() },
            original with { DueAt = Epoch.AddMinutes(1) },
            original with { Body = new byte[] { 1, 9 } },
            original with { Metadata = new byte[] { 3, 9 } },
        ];

        foreach (SerializedDurableSend conflict in conflicts)
        {
            DurableSendIdentityConflictException exception = await Assert.ThrowsAsync<DurableSendIdentityConflictException>(
                () => store.AdmitAsync(conflict, new DurableSendStoreLimits(20, 1000), Epoch));
            Assert.Equal(original.Id, exception.Id);
        }

        Assert.Equal(1, (await store.GetSnapshotAsync()).StoredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-STORE-CAPACITY", "atomic-concurrent-count-bound")]
    public async Task Admission_ConcurrentWritersCannotOvershootCountCapacityAsync()
    {
        StoreHarness store = Store();
        var limits = new DurableSendStoreLimits(10, 10_000);
        Task<bool>[] attempts = Enumerable.Range(0, 100)
            .Select(index => Task.Run(async () =>
            {
                try
                {
                    await store.AdmitAsync(Message(GuidFrom(index + 1), body: [1]), limits, Epoch);
                    return true;
                }
                catch (DurableSendCapacityExceededException)
                {
                    return false;
                }
            }))
            .ToArray();

        bool[] outcomes = await Task.WhenAll(attempts);
        DurableSendStoreSnapshot snapshot = await store.GetSnapshotAsync();

        Assert.Equal(10, outcomes.Count(static accepted => accepted));
        Assert.Equal(10, snapshot.StoredCount);
        Assert.Equal(10, snapshot.StoredBytes);
        Assert.Equal(10, snapshot.PendingCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-STORE-CAPACITY", "exact-logical-byte-bound-and-zero-body")]
    public async Task Admission_UsesExactBodyPlusMetadataBytesAndStillCountBoundsZeroByteRecordsAsync()
    {
        StoreHarness bytesStore = Store();
        var byteLimits = new DurableSendStoreLimits(10, 10);
        await bytesStore.AdmitAsync(Message(body: new byte[7], metadata: new byte[3]), byteLimits, Epoch);

        DurableSendCapacityExceededException bytesException = await Assert.ThrowsAsync<DurableSendCapacityExceededException>(
            () => bytesStore.AdmitAsync(Message(GuidFrom(2), body: [1]), byteLimits, Epoch));
        Assert.Equal(1, bytesException.StoredCount);
        Assert.Equal(10, bytesException.StoredBytes);

        StoreHarness countStore = Store();
        var countLimits = new DurableSendStoreLimits(1, 1);
        DurableSendAdmissionResult zero = await countStore.AdmitAsync(
            Message(body: [], metadata: []),
            countLimits,
            Epoch);
        Assert.Equal(0, zero.StoredBytes);
        await Assert.ThrowsAsync<DurableSendCapacityExceededException>(() =>
            countStore.AdmitAsync(Message(GuidFrom(3), body: [], metadata: []), countLimits, Epoch));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-STORE-SNAPSHOT", "caller-buffers-copied-before-success")]
    public async Task Admission_SnapshotsCallerOwnedBodyAndMetadataBuffersAsync()
    {
        StoreHarness store = Store();
        byte[] body = [1, 2, 3];
        byte[] metadata = [4, 5];
        SerializedDurableSend message = Message(body: body, metadata: metadata);

        await store.AdmitAsync(message, new DurableSendStoreLimits(10, 100), Epoch);
        body[0] = 99;
        metadata[0] = 88;

        DurableSendDelivery claimed = Assert.Single(await store.ClaimDueAsync(Epoch, 1, TimeSpan.FromMinutes(1)));
        Assert.Equal(new byte[] { 1, 2, 3 }, claimed.Message.Body.ToArray());
        Assert.Equal(new byte[] { 4, 5 }, claimed.Message.Metadata.ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-STORE-LEASE", "exclusive-lease-expiry-and-takeover")]
    public async Task Claim_ExcludesLiveLeaseAndIssuesANewTokenAtExpiryAsync()
    {
        StoreHarness store = Store();
        SerializedDurableSend message = Message();
        await store.AdmitAsync(message, new DurableSendStoreLimits(10, 100), Epoch);

        DurableSendDelivery first = Assert.Single(await store.ClaimDueAsync(Epoch, 1, TimeSpan.FromMinutes(1)));
        Assert.Empty(await store.ClaimDueAsync(Epoch.AddTicks(1), 1, TimeSpan.FromMinutes(1)));
        DurableSendDelivery second = Assert.Single(await store.ClaimDueAsync(
            Epoch.AddMinutes(1),
            1,
            TimeSpan.FromMinutes(2)));

        Assert.NotEqual(first.Lease.Token, second.Lease.Token);
        Assert.Equal(Epoch.AddMinutes(3), second.Lease.ExpiresAt);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.MarkDeliveredAsync(message.Id, first.Lease, Epoch.AddMinutes(1)));
        Assert.True(await store.MarkDeliveredAsync(message.Id, second.Lease, Epoch.AddMinutes(1)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-STORE-LEASE", "bounded-ordered-claim-and-input-validation")]
    public async Task Claim_IsBoundedOrderedAndRejectsUnsafeRequestsAsync()
    {
        StoreHarness store = Store();
        SerializedDurableSend later = Message(GuidFrom(2));
        SerializedDurableSend sameTimeHighId = Message(GuidFrom(3));
        SerializedDurableSend sameTimeLowId = Message(GuidFrom(1));
        var limits = new DurableSendStoreLimits(10, 100);
        await store.AdmitAsync(later, limits, Epoch.AddMinutes(1));
        await store.AdmitAsync(sameTimeHighId, limits, Epoch);
        await store.AdmitAsync(sameTimeLowId, limits, Epoch);

        IReadOnlyList<DurableSendDelivery> claimed = await store.ClaimDueAsync(Epoch.AddMinutes(2), 2, TimeSpan.FromSeconds(30));

        Assert.Equal([sameTimeLowId.Id, sameTimeHighId.Id], claimed.Select(item => item.Message.Id));
        Assert.Equal(2, claimed.Count);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            store.ClaimDueAsync(Epoch, 0, TimeSpan.FromSeconds(1)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => store.ClaimDueAsync(
            Epoch,
            DurableSendOperationLimits.AbsoluteMaximumClaimCount + 1,
            TimeSpan.FromSeconds(1)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            store.ClaimDueAsync(Epoch, 1, TimeSpan.Zero));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-STORE-RETRY", "retry-state-evidence-and-due-time")]
    public async Task Retry_PersistsBoundedEvidenceAndBecomesClaimableOnlyWhenDueAsync()
    {
        StoreHarness store = Store();
        SerializedDurableSend message = Message();
        await store.AdmitAsync(message, new DurableSendStoreLimits(10, 100), Epoch);
        DurableSendDelivery first = Assert.Single(await store.ClaimDueAsync(Epoch, 1, TimeSpan.FromMinutes(1)));
        string longFailureType = new('f', 700);

        Assert.True(await store.ScheduleRetryAsync(
            message.Id,
            first.Lease,
            2,
            Epoch.AddMinutes(5),
            DurableSendFailureKind.Transient,
            longFailureType,
            Epoch));
        DurableSendStoreSnapshot snapshot = await store.GetSnapshotAsync();
        Assert.Equal(1, snapshot.RetryScheduledCount);
        Assert.Empty(await store.ClaimDueAsync(Epoch.AddMinutes(5).AddTicks(-1), 1, TimeSpan.FromMinutes(1)));

        DurableSendDelivery retry = Assert.Single(await store.ClaimDueAsync(Epoch.AddMinutes(5), 1, TimeSpan.FromMinutes(1)));
        Assert.Equal(2, retry.DeliveryAttempts);
        Assert.Equal(DurableSendStatus.RetryScheduled, retry.Status);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-STORE-QUARANTINE", "retained-capacity-requeue-and-single-release")]
    public async Task Quarantine_RetainsCapacityUntilDiscardAndRequeueDoesNotDoubleChargeAsync()
    {
        StoreHarness store = Store();
        var limits = new DurableSendStoreLimits(1, 8);
        SerializedDurableSend message = Message(body: new byte[8]);
        await store.AdmitAsync(message, limits, Epoch);
        DurableSendDelivery delivery = Assert.Single(await store.ClaimDueAsync(Epoch, 1, TimeSpan.FromMinutes(1)));
        Assert.True(await store.QuarantineAsync(
            message.Id,
            delivery.Lease,
            1,
            DurableSendFailureKind.NonRetryable,
            "Tests.Permanent",
            Epoch));

        DurableSendAdmissionResult duplicate = await store.AdmitAsync(message, limits, Epoch);
        Assert.Equal(DurableSendAdmissionDisposition.AlreadyQuarantined, duplicate.Disposition);
        await Assert.ThrowsAsync<DurableSendCapacityExceededException>(() =>
            store.AdmitAsync(Message(GuidFrom(2), body: []), limits, Epoch));
        Assert.Equal(
            DurableSendOperationOutcome.Requeued,
            (await store.RequeueAsync(message.Id, Epoch.AddMinutes(1))).Outcome);
        DurableSendStoreSnapshot requeued = await store.GetSnapshotAsync();
        Assert.Equal(1, requeued.StoredCount);
        Assert.Equal(8, requeued.StoredBytes);
        Assert.Equal(0, requeued.QuarantinedCount);
        Assert.Equal(
            DurableSendOperationOutcome.NotQuarantined,
            (await store.DiscardQuarantinedAsync(message.Id)).Outcome);

        DurableSendDelivery retry = Assert.Single(await store.ClaimDueAsync(
            Epoch.AddMinutes(1),
            1,
            TimeSpan.FromMinutes(1)));
        Assert.True(await store.QuarantineAsync(
            message.Id,
            retry.Lease,
            1,
            DurableSendFailureKind.NonRetryable,
            null,
            Epoch.AddMinutes(1)));
        Assert.Equal(
            DurableSendOperationOutcome.Discarded,
            (await store.DiscardQuarantinedAsync(message.Id)).Outcome);
        Assert.Equal(
            DurableSendOperationOutcome.NotFound,
            (await store.DiscardQuarantinedAsync(message.Id)).Outcome);
        Assert.Equal(0, (await store.GetSnapshotAsync()).StoredBytes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-STORE-COMPLETION", "generation-fenced-late-completion")]
    public async Task Completion_ValidGenerationWinsQuarantineRaceButStaleGenerationCannotRetireReadmissionAsync()
    {
        StoreHarness store = Store();
        SerializedDurableSend message = Message();
        var limits = new DurableSendStoreLimits(1, 100);
        await store.AdmitAsync(message, limits, Epoch);
        DurableSendDelivery first = Assert.Single(await store.ClaimDueAsync(Epoch, 1, TimeSpan.FromMinutes(1)));
        Assert.True(await store.QuarantineAsync(
            message.Id,
            first.Lease,
            1,
            DurableSendFailureKind.ConsumerCompletionTimeout,
            null,
            Epoch));
        Assert.True(await store.CompleteConsumerDeliveryAsync(message.Id, first.GenerationToken, Epoch));

        await store.AdmitAsync(message, limits, Epoch.AddMinutes(1));
        DurableSendDelivery second = Assert.Single(await store.ClaimDueAsync(
            Epoch.AddMinutes(1),
            1,
            TimeSpan.FromMinutes(1)));
        Assert.NotEqual(first.GenerationToken, second.GenerationToken);
        Assert.False(await store.CompleteConsumerDeliveryAsync(message.Id, first.GenerationToken, Epoch));
        Assert.Equal(1, (await store.GetSnapshotAsync()).StoredCount);
        Assert.True(await store.CompleteConsumerDeliveryAsync(message.Id, second.GenerationToken, Epoch));
        Assert.False(await store.CompleteConsumerDeliveryAsync(message.Id, second.GenerationToken, Epoch));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-STORE-COMPLETION", "early-completion-and-await-transition-race")]
    public async Task Completion_MayRetireBeforeTheAwaitingTransitionIsPersistedAsync()
    {
        StoreHarness store = Store();
        SerializedDurableSend message = Message();
        await store.AdmitAsync(message, new DurableSendStoreLimits(1, 100), Epoch);
        DurableSendDelivery delivery = Assert.Single(await store.ClaimDueAsync(Epoch, 1, TimeSpan.FromMinutes(1)));

        Assert.True(await store.CompleteConsumerDeliveryAsync(message.Id, delivery.GenerationToken, Epoch));
        Assert.False(await store.AwaitConsumerCompletionAsync(
            message.Id,
            delivery.Lease,
            1,
            Epoch.AddMinutes(5)));
        Assert.Equal(0, (await store.GetSnapshotAsync()).StoredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-STORE-QUARANTINE", "bounded-payload-free-ordered-page")]
    public async Task QuarantineQuery_ReturnsOnlyTheBoundedNewestPayloadFreeEvidenceAsync()
    {
        StoreHarness store = Store();
        var limits = new DurableSendStoreLimits(10, 100);
        SerializedDurableSend firstMessage = Message(GuidFrom(1), body: [1]);
        SerializedDurableSend secondMessage = Message(GuidFrom(2), body: [2]);
        await store.AdmitAsync(firstMessage, limits, Epoch);
        await store.AdmitAsync(secondMessage, limits, Epoch);
        IReadOnlyList<DurableSendDelivery> deliveries = await store.ClaimDueAsync(Epoch, 2, TimeSpan.FromMinutes(1));
        DurableSendDelivery first = Assert.Single(deliveries, item => item.Message.Id == firstMessage.Id);
        DurableSendDelivery second = Assert.Single(deliveries, item => item.Message.Id == secondMessage.Id);
        await store.QuarantineAsync(firstMessage.Id, first.Lease, 1, DurableSendFailureKind.Unclassified,
            "First", Epoch);
        await store.QuarantineAsync(secondMessage.Id, second.Lease, 2, DurableSendFailureKind.NonRetryable,
            new string('x', 700), Epoch.AddMinutes(1));

        DurableSendQuarantineEntry evidence = Assert.Single(
            (await store.GetQuarantineAsync(DurableSendQuarantineQuery.FirstPage(1))).Entries);
        Assert.Equal(secondMessage.Id, evidence.Id);
        Assert.Equal(2, evidence.DeliveryAttempts);
        Assert.Equal(DurableSendFailureKind.NonRetryable, evidence.FailureKind);
        Assert.Equal(512, evidence.FailureType!.Length);
        Assert.DoesNotContain("Body", evidence.GetType().GetProperties().Select(property => property.Name));
        DurableSendQuarantinePage exactPage = await store.GetQuarantineAsync(DurableSendQuarantineQuery.FirstPage(2));
        Assert.Equal(2, exactPage.Entries.Count);
        Assert.False(exactPage.HasMore);
        Assert.Null(exactPage.ContinuationToken);
        Assert.Null(exactPage.NextQuery);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            store.GetQuarantineAsync(DurableSendQuarantineQuery.FirstPage(0)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => store.GetQuarantineAsync(
            DurableSendQuarantineQuery.FirstPage(
                DurableSendOperationLimits.AbsoluteMaximumQuarantinePageSize + 1)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-STORE-PAGINATION", "complete-seek-traversal-with-concurrent-changes")]
    public async Task QuarantinePagination_TraversesMoreThanOneThousandEqualTimestampsWithoutDuplicatesOrGapsAsync()
    {
        const int retainedCount = 1005;
        StoreHarness store = Store();
        var limits = new DurableSendStoreLimits(retainedCount + 1, retainedCount + 1);
        var expected = new List<DurableSendId>(retainedCount);
        for (var index = 1; index <= retainedCount; index++)
        {
            SerializedDurableSend message = Message(GuidFrom(index));
            expected.Add(message.Id);
            await store.AdmitAsync(message, limits, Epoch);
        }

        while ((await store.GetSnapshotAsync()).PendingCount > 0)
        {
            IReadOnlyList<DurableSendDelivery> deliveries = await store.ClaimDueAsync(
                Epoch,
                DurableSendOperationLimits.AbsoluteMaximumClaimCount,
                TimeSpan.FromMinutes(1));
            foreach (DurableSendDelivery delivery in deliveries)
            {
                Assert.True(await store.QuarantineAsync(
                    delivery.Message.Id,
                    delivery.Lease,
                    1,
                    DurableSendFailureKind.NonRetryable,
                    "Tests.Page",
                    Epoch));
            }
        }

        var actual = new List<DurableSendId>(retainedCount);
        DurableSendQuarantinePage page = await store.GetQuarantineAsync(DurableSendQuarantineQuery.FirstPage(137));
        actual.AddRange(page.Entries.Select(entry => entry.Id));
        Assert.True(page.HasMore);
        string versionedToken = Assert.IsType<string>(page.ContinuationToken);
        Assert.NotNull(page.NextQuery);
        Assert.Equal(137, page.NextQuery!.PageSize);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<DurableSendQuarantineEntry>)page.Entries).RemoveAt(0));

        // Entries already returned remain a stable part of this traversal when operators mutate them concurrently.
        Assert.Equal(DurableSendOperationOutcome.Requeued,
            (await store.RequeueAsync(actual[0], Epoch.AddMinutes(1))).Outcome);
        Assert.Equal(DurableSendOperationOutcome.Discarded,
            (await store.DiscardQuarantinedAsync(actual[1])).Outcome);

        // A newer concurrent insert belongs to a fresh traversal, not behind the existing seek cursor.
        SerializedDurableSend inserted = Message(GuidFrom(retainedCount + 1));
        await store.AdmitAsync(inserted, limits, Epoch);
        DurableSendDelivery insertedDelivery = Assert.Single(await store.ClaimDueAsync(
            Epoch,
            1,
            TimeSpan.FromMinutes(1)));
        Assert.True(await store.QuarantineAsync(
            inserted.Id,
            insertedDelivery.Lease,
            1,
            DurableSendFailureKind.NonRetryable,
            "Tests.Concurrent",
            Epoch.AddMinutes(1)));

        while (page.NextQuery is { } next)
        {
            page = await store.GetQuarantineAsync(next);
            actual.AddRange(page.Entries.Select(entry => entry.Id));
        }

        Assert.Equal(expected, actual);
        Assert.Equal(retainedCount, actual.Distinct().Count());
        Assert.Equal(inserted.Id,
            Assert.Single((await store.GetQuarantineAsync(DurableSendQuarantineQuery.FirstPage(1))).Entries).Id);
        Assert.Equal(DurableSendOperationOutcome.NotQuarantined,
            (await store.RequeueAsync(expected[0], Epoch)).Outcome);
        Assert.Equal(DurableSendOperationOutcome.NotFound,
            (await store.DiscardQuarantinedAsync(new DurableSendId(Guid.NewGuid()))).Outcome);
        await Assert.ThrowsAsync<ArgumentException>(() => store.GetQuarantineAsync(new DurableSendQuarantineQuery
        {
            PageSize = 10,
            ContinuationToken = "not-a-valid-token",
        }));
        string paddedToken = versionedToken.Replace('-', '+').Replace('_', '/');
        paddedToken += new string('=', (4 - paddedToken.Length % 4) % 4);
        byte[] unsupportedVersion = Convert.FromBase64String(paddedToken);
        unsupportedVersion[0]++;
        string unsupportedVersionToken = Convert.ToBase64String(unsupportedVersion)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        await Assert.ThrowsAsync<ArgumentException>(() => store.GetQuarantineAsync(new DurableSendQuarantineQuery
        {
            PageSize = 10,
            ContinuationToken = unsupportedVersionToken,
        }));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-SCHEDULE", "scheduled-outbox-intent-is-hidden-until-fake-clock-reaches-due-time")]
    public async Task Schedule_BecomesClaimableAtDueTimeAndCanBeCanceledBeforeClaimAsync()
    {
        StoreHarness store = Store();
        var clock = new FakeTimeProvider(Epoch);
        var limits = new DurableSendStoreLimits(10, 100);
        SerializedDurableSend dueMessage = Message(GuidFrom(1));
        SerializedDurableSend cancelledMessage = Message(GuidFrom(2));
        DateTimeOffset dueAt = Epoch.AddMinutes(15);

        DurableSendAdmissionResult scheduled = await store.Schedule.ScheduleAsync(
            dueMessage,
            limits,
            clock.GetUtcNow(),
            dueAt,
            TestContext.Current.CancellationToken);
        await store.Schedule.ScheduleAsync(
            cancelledMessage,
            limits,
            clock.GetUtcNow(),
            dueAt.AddMinutes(1),
            TestContext.Current.CancellationToken);

        Assert.True(scheduled.IsNew);
        Assert.Empty(await store.ClaimDueAsync(clock.GetUtcNow(), 10, TimeSpan.FromMinutes(1)));
        ReliableMessagingOperationResult cancelled = await store.Schedule.CancelAsync(
            cancelledMessage.Id,
            TestContext.Current.CancellationToken);
        Assert.Equal(ReliableMessagingOperationDisposition.Applied, cancelled.Disposition);
        Assert.Equal("Canceled", cancelled.CurrentState);

        clock.Advance(TimeSpan.FromMinutes(15) - TimeSpan.FromTicks(1));
        Assert.Empty(await store.ClaimDueAsync(clock.GetUtcNow(), 10, TimeSpan.FromMinutes(1)));
        clock.Advance(TimeSpan.FromTicks(1));

        DurableSendDelivery delivery = Assert.Single(await store.ClaimDueAsync(
            clock.GetUtcNow(),
            10,
            TimeSpan.FromMinutes(1)));
        Assert.Equal(dueMessage.Id, delivery.Message.Id);
        Assert.Equal(dueAt, delivery.Message.DueAt);
        ReliableMessagingOperationResult claimed = await store.Schedule.CancelAsync(
            dueMessage.Id,
            TestContext.Current.CancellationToken);
        Assert.Equal(ReliableMessagingOperationDisposition.InvalidState, claimed.Disposition);
        Assert.Equal(1, (await store.GetSnapshotAsync()).StoredCount);
        Assert.Equal(
            ReliableMessagingOperationDisposition.NotFound,
            (await store.Schedule.CancelAsync(cancelledMessage.Id, TestContext.Current.CancellationToken)).Disposition);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "duplicate-fencing-retry-due-time-and-terminal-completion")]
    public async Task Inbox_DuplicateAndRetryTransitionsAreDueAndLeaseFencedAsync()
    {
        StoreHarness store = Store();
        var clock = new FakeTimeProvider(Epoch);
        var key = new ReliableInboxKey(GuidFrom(11), GuidFrom(12));

        ReliableInboxAcquireResult first = await store.Inbox.AcquireAsync(
            key,
            clock.GetUtcNow(),
            TimeSpan.FromMinutes(1),
            TestContext.Current.CancellationToken);
        Assert.Equal(ReliableInboxAcquireDisposition.Acquired, first.Disposition);
        Assert.Equal(1, first.Attempt);
        ReliableInboxLease firstLease = Assert.IsType<ReliableInboxLease>(first.Lease);

        ReliableInboxAcquireResult busy = await store.Inbox.AcquireAsync(
            key,
            clock.GetUtcNow().AddTicks(1),
            TimeSpan.FromMinutes(1),
            TestContext.Current.CancellationToken);
        Assert.Equal(ReliableInboxAcquireDisposition.Busy, busy.Disposition);
        Assert.Null(busy.Lease);

        DateTimeOffset retryAt = Epoch.AddMinutes(5);
        Assert.True(await store.Inbox.ScheduleRetryAsync(
            key,
            firstLease,
            retryAt,
            "Tests.Transient",
            clock.GetUtcNow(),
            TestContext.Current.CancellationToken));
        Assert.Equal(
            ReliableInboxAcquireDisposition.NotDue,
            (await store.Inbox.AcquireAsync(
                key,
                retryAt.AddTicks(-1),
                TimeSpan.FromMinutes(1),
                TestContext.Current.CancellationToken)).Disposition);

        clock.Advance(TimeSpan.FromMinutes(5));
        ReliableInboxAcquireResult retry = await store.Inbox.AcquireAsync(
            key,
            clock.GetUtcNow(),
            TimeSpan.FromMinutes(1),
            TestContext.Current.CancellationToken);
        Assert.Equal(ReliableInboxAcquireDisposition.Acquired, retry.Disposition);
        Assert.Equal(2, retry.Attempt);
        ReliableInboxLease retryLease = Assert.IsType<ReliableInboxLease>(retry.Lease);
        Assert.NotEqual(firstLease.Token, retryLease.Token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.Inbox.CompleteAsync(
            key,
            firstLease,
            clock.GetUtcNow(),
            TestContext.Current.CancellationToken));
        Assert.True(await store.Inbox.CompleteAsync(
            key,
            retryLease,
            clock.GetUtcNow(),
            TestContext.Current.CancellationToken));

        ReliableInboxAcquireResult duplicate = await store.Inbox.AcquireAsync(
            key,
            clock.GetUtcNow().AddYears(1),
            TimeSpan.FromMinutes(1),
            TestContext.Current.CancellationToken);
        Assert.Equal(ReliableInboxAcquireDisposition.AlreadyConsumed, duplicate.Disposition);
        Assert.Equal(2, duplicate.Attempt);
        Assert.Null(duplicate.Lease);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX-QUARANTINE", "seek-pagination-over-one-thousand-and-explicit-terminal-actions")]
    public async Task InboxQuarantine_TraversesMoreThanOneThousandRowsAndAppliesExplicitOperatorActionsAsync()
    {
        const int retainedCount = 1005;
        StoreHarness store = Store();
        Guid consumerId = GuidFrom(2000);
        var expected = new List<ReliableInboxKey>(retainedCount);

        for (var index = 1; index <= retainedCount; index++)
        {
            var key = new ReliableInboxKey(GuidFrom(index), consumerId);
            expected.Add(key);
            ReliableInboxAcquireResult acquired = await store.Inbox.AcquireAsync(
                key,
                Epoch,
                TimeSpan.FromMinutes(1),
                TestContext.Current.CancellationToken);
            Assert.True(await store.Inbox.QuarantineAsync(
                key,
                Assert.IsType<ReliableInboxLease>(acquired.Lease),
                new string('x', 700),
                Epoch,
                TestContext.Current.CancellationToken));
        }

        var actual = new List<ReliableInboxKey>(retainedCount);
        var query = new ReliableInboxQuarantineQuery { PageSize = 137 };
        while (true)
        {
            ReliableInboxQuarantinePage page = await store.Inbox.GetQuarantineAsync(
                query,
                TestContext.Current.CancellationToken);
            actual.AddRange(page.Entries.Select(entry => entry.Key));
            Assert.All(page.Entries, entry =>
            {
                Assert.Equal(ReliableInboxStatus.Quarantined, entry.Status);
                Assert.Equal(1, entry.Attempts);
                Assert.Equal(512, entry.FailureType!.Length);
            });
            if (page.Next is null)
                break;
            query = page.Next;
        }

        Assert.Equal(expected, actual);
        Assert.Equal(retainedCount, actual.Distinct().Count());
        Assert.Equal(
            ReliableMessagingOperationDisposition.Applied,
            (await store.Inbox.AbandonAsync(expected[0], Epoch.AddMinutes(1), TestContext.Current.CancellationToken)).Disposition);
        Assert.Equal(
            ReliableMessagingOperationDisposition.Applied,
            (await store.Inbox.RequeueAsync(expected[1], Epoch.AddMinutes(1), TestContext.Current.CancellationToken)).Disposition);
        Assert.Equal(
            ReliableMessagingOperationDisposition.Applied,
            (await store.Inbox.DiscardAsync(expected[2], TestContext.Current.CancellationToken)).Disposition);
        Assert.Equal(
            ReliableMessagingOperationDisposition.InvalidState,
            (await store.Inbox.AbandonAsync(expected[0], Epoch.AddMinutes(2), TestContext.Current.CancellationToken)).Disposition);

        ReliableInboxQuarantinePage remaining = await store.Inbox.GetQuarantineAsync(
            new ReliableInboxQuarantineQuery { PageSize = 1000 },
            TestContext.Current.CancellationToken);
        Assert.Equal(1000, remaining.Entries.Count);
        Assert.DoesNotContain(remaining.Entries, entry =>
            entry.Key == expected[0] || entry.Key == expected[1] || entry.Key == expected[2]);
        await Assert.ThrowsAsync<ArgumentException>(() => store.Inbox.GetQuarantineAsync(
            new ReliableInboxQuarantineQuery { PageSize = 10, AfterQuarantinedAt = Epoch },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-STORE-CANCELLATION", "pre-canceled-operations-do-not-mutate")]
    public async Task Operations_PreCanceledTokenIsPreservedWithoutMutationAsync()
    {
        StoreHarness store = Store();
        using var source = new CancellationTokenSource();
        source.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            store.AdmitCanceledAsync(Message(), new DurableSendStoreLimits(1, 100), Epoch, source.Token));

        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.Equal(0, (await store.GetSnapshotAsync()).StoredCount);
    }

    private static StoreHarness Store() => new(DurableSenderTestFactory.CreateInMemoryStore<ITestBus>());

    private static SerializedDurableSend Message(
        Guid? id = null,
        byte[]? body = null,
        byte[]? metadata = null,
        Guid? messageId = null,
        Guid? correlationId = null)
        => new()
        {
            Id = new DurableSendId(id ?? GuidFrom(1)),
            ContractIdentity = new MessageContractIdentity("vicione.tests.durable-store", 1),
            DestinationAddress = new Uri("loopback://durable-store"),
            ContentType = "application/octet-stream",
            Body = body ?? [1],
            Metadata = metadata ?? [],
            MessageId = messageId,
            CorrelationId = correlationId,
        };

    private static Guid GuidFrom(int value) => new(value, 0, 0, new byte[8]);

    private interface ITestBus : IBus;

    private sealed class StoreHarness(IOutboxStore<ITestBus> store)
    {
        public IInboxStore<ITestBus> Inbox { get; } = Assert.IsAssignableFrom<IInboxStore<ITestBus>>(store);

        public IScheduleStore<ITestBus> Schedule { get; } = Assert.IsAssignableFrom<IScheduleStore<ITestBus>>(store);

        public Task<DurableSendAdmissionResult> AdmitAsync(
            SerializedDurableSend message,
            DurableSendStoreLimits limits,
            DateTimeOffset enqueuedAt) =>
            store.AdmitAsync(message, limits, enqueuedAt, TestContext.Current.CancellationToken);

        public Task<DurableSendAdmissionResult> AdmitCanceledAsync(
            SerializedDurableSend message,
            DurableSendStoreLimits limits,
            DateTimeOffset enqueuedAt,
            CancellationToken cancellationToken) =>
            store.AdmitAsync(message, limits, enqueuedAt, cancellationToken);

        public Task<IReadOnlyList<DurableSendDelivery>> ClaimDueAsync(
            DateTimeOffset now,
            int maximumCount,
            TimeSpan leaseDuration) =>
            store.ClaimDueAsync(now, maximumCount, leaseDuration, TestContext.Current.CancellationToken);

        public Task<bool> MarkDeliveredAsync(DurableSendId id, DurableSendLease lease, DateTimeOffset deliveredAt) =>
            store.MarkDeliveredAsync(id, lease, deliveredAt, TestContext.Current.CancellationToken);

        public Task<bool> AwaitConsumerCompletionAsync(
            DurableSendId id,
            DurableSendLease lease,
            int deliveryAttempts,
            DateTimeOffset nextAttemptAt) =>
            store.AwaitConsumerCompletionAsync(
                id,
                lease,
                deliveryAttempts,
                nextAttemptAt,
                TestContext.Current.CancellationToken);

        public Task<bool> CompleteConsumerDeliveryAsync(
            DurableSendId id,
            Guid generationToken,
            DateTimeOffset completedAt) =>
            store.CompleteConsumerDeliveryAsync(
                id,
                generationToken,
                completedAt,
                TestContext.Current.CancellationToken);

        public Task<bool> ScheduleRetryAsync(
            DurableSendId id,
            DurableSendLease lease,
            int deliveryAttempts,
            DateTimeOffset nextAttemptAt,
            DurableSendFailureKind failureKind,
            string? failureType,
            DateTimeOffset failedAt) =>
            store.ScheduleRetryAsync(
                id,
                lease,
                deliveryAttempts,
                nextAttemptAt,
                failureKind,
                failureType,
                failedAt,
                TestContext.Current.CancellationToken);

        public Task<bool> QuarantineAsync(
            DurableSendId id,
            DurableSendLease lease,
            int deliveryAttempts,
            DurableSendFailureKind failureKind,
            string? failureType,
            DateTimeOffset quarantinedAt) =>
            store.QuarantineAsync(
                id,
                lease,
                deliveryAttempts,
                failureKind,
                failureType,
                quarantinedAt,
                TestContext.Current.CancellationToken);

        public Task<DurableSendStoreSnapshot> GetSnapshotAsync() =>
            store.GetSnapshotAsync(TestContext.Current.CancellationToken);

        public Task<DurableSendQuarantinePage> GetQuarantineAsync(DurableSendQuarantineQuery query) =>
            store.GetQuarantineAsync(query, TestContext.Current.CancellationToken);

        public Task<DurableSendOperationResult> RequeueAsync(DurableSendId id, DateTimeOffset dueAt) =>
            store.RequeueAsync(id, dueAt, TestContext.Current.CancellationToken);

        public Task<DurableSendOperationResult> DiscardQuarantinedAsync(DurableSendId id) =>
            store.DiscardQuarantinedAsync(id, TestContext.Current.CancellationToken);
    }
}
