using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.DurableSend;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DurableSend;

public sealed class InMemoryDurableSendStoreTests
{
    private static readonly DateTimeOffset Epoch = DateTimeOffset.Parse("2026-09-03T12:00:00+00:00");

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-STORE-ADMISSION", "exact-idempotence-without-second-capacity-charge")]
    public async Task Admission_ExactDuplicateIsIdempotentWithoutASecondCapacityCharge()
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
    public async Task Admission_SameIdWithAnyDifferentImmutableIntentFailsLoudly()
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
    public async Task Admission_ConcurrentWritersCannotOvershootCountCapacity()
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
    public async Task Admission_UsesExactBodyPlusMetadataBytesAndStillCountBoundsZeroByteRecords()
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
    public async Task Admission_SnapshotsCallerOwnedBodyAndMetadataBuffers()
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
    public async Task Claim_ExcludesLiveLeaseAndIssuesANewTokenAtExpiry()
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
    public async Task Claim_IsBoundedOrderedAndRejectsUnsafeRequests()
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
    public async Task Retry_PersistsBoundedEvidenceAndBecomesClaimableOnlyWhenDue()
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
    public async Task Quarantine_RetainsCapacityUntilDiscardAndRequeueDoesNotDoubleCharge()
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
        Assert.True(await store.RequeueAsync(message.Id, Epoch.AddMinutes(1)));
        DurableSendStoreSnapshot requeued = await store.GetSnapshotAsync();
        Assert.Equal(1, requeued.StoredCount);
        Assert.Equal(8, requeued.StoredBytes);
        Assert.Equal(0, requeued.QuarantinedCount);

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
        Assert.True(await store.DiscardQuarantinedAsync(message.Id));
        Assert.False(await store.DiscardQuarantinedAsync(message.Id));
        Assert.Equal(0, (await store.GetSnapshotAsync()).StoredBytes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-STORE-COMPLETION", "generation-fenced-late-completion")]
    public async Task Completion_ValidGenerationWinsQuarantineRaceButStaleGenerationCannotRetireReadmission()
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
    public async Task Completion_MayRetireBeforeTheAwaitingTransitionIsPersisted()
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
    public async Task QuarantineQuery_ReturnsOnlyTheBoundedNewestPayloadFreeEvidence()
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

        DurableSendQuarantineEntry evidence = Assert.Single(await store.GetQuarantineAsync(1));
        Assert.Equal(secondMessage.Id, evidence.Id);
        Assert.Equal(2, evidence.DeliveryAttempts);
        Assert.Equal(DurableSendFailureKind.NonRetryable, evidence.FailureKind);
        Assert.Equal(512, evidence.FailureType!.Length);
        Assert.DoesNotContain("Body", evidence.GetType().GetProperties().Select(property => property.Name));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => store.GetQuarantineAsync(0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => store.GetQuarantineAsync(
            DurableSendOperationLimits.AbsoluteMaximumQuarantinePageSize + 1));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-STORE-CANCELLATION", "pre-canceled-operations-do-not-mutate")]
    public async Task Operations_PreCanceledTokenIsPreservedWithoutMutation()
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

    private sealed class StoreHarness(IDurableSendStore<ITestBus> store)
    {
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

        public Task<IReadOnlyList<DurableSendQuarantineEntry>> GetQuarantineAsync(int maximumCount) =>
            store.GetQuarantineAsync(maximumCount, TestContext.Current.CancellationToken);

        public Task<bool> RequeueAsync(DurableSendId id, DateTimeOffset dueAt) =>
            store.RequeueAsync(id, dueAt, TestContext.Current.CancellationToken);

        public Task<bool> DiscardQuarantinedAsync(DurableSendId id) =>
            store.DiscardQuarantinedAsync(id, TestContext.Current.CancellationToken);
    }
}
