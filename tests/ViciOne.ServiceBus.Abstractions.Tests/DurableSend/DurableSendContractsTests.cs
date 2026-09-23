using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.DurableSend;

public sealed class DurableSendContractsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-IDENTITY", "nonempty-id-and-lease-token")]
    public void IdAndLease_RequireNonemptyStableIdentities()
    {
        Guid idValue = Guid.Parse("11111111-2222-3333-4444-555555555555");
        DateTimeOffset expiresAt = DateTimeOffset.Parse("2026-09-03T12:00:00+00:00");

        var id = new DurableSendId(idValue);
        var lease = new DurableSendLease(idValue, expiresAt);
        var inboxLease = new ReliableInboxLease(idValue, expiresAt);

        Assert.Equal(idValue, id.Value);
        Assert.Equal("11111111-2222-3333-4444-555555555555", id.ToString());
        Assert.Equal(idValue, lease.Token);
        Assert.Equal(expiresAt, lease.ExpiresAt);
        Assert.Equal(idValue, inboxLease.Token);
        Assert.Equal(expiresAt, inboxLease.ExpiresAt);
        Assert.Equal("value", Assert.Throws<ArgumentException>(() => new DurableSendId(Guid.Empty)).ParamName);
        Assert.Equal("token", Assert.Throws<ArgumentException>(() => new DurableSendLease(Guid.Empty, expiresAt)).ParamName);
        Assert.Equal("token", Assert.Throws<ArgumentException>(() => new ReliableInboxLease(Guid.Empty, expiresAt)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-CAPACITY", "positive-exact-store-limits")]
    public void StoreLimits_RequirePositiveExactCountAndLogicalByteBounds()
    {
        var minimum = new DurableSendStoreLimits(1, 1);

        Assert.Equal(1, minimum.MaximumStoredCount);
        Assert.Equal(1, minimum.MaximumStoredBytes);
        Assert.Equal("maximumStoredCount", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DurableSendStoreLimits(0, 1)).ParamName);
        Assert.Equal("maximumStoredBytes", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DurableSendStoreLimits(1, 0)).ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => new DurableSendStoreLimits(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DurableSendStoreLimits(1, -1));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-OPERATIONS", "absolute-claim-and-page-bounds")]
    public void OperationLimits_AcceptOnlyThePublishedFiniteRanges()
    {
        Assert.Equal(1, DurableSendOperationLimits.ValidateClaimCount(1, "claim"));
        Assert.Equal(DurableSendOperationLimits.AbsoluteMaximumClaimCount,
            DurableSendOperationLimits.ValidateClaimCount(DurableSendOperationLimits.AbsoluteMaximumClaimCount, "claim"));
        Assert.Equal(1, DurableSendOperationLimits.ValidateQuarantinePageSize(1, "page"));
        Assert.Equal(DurableSendOperationLimits.AbsoluteMaximumQuarantinePageSize,
            DurableSendOperationLimits.ValidateQuarantinePageSize(
                DurableSendOperationLimits.AbsoluteMaximumQuarantinePageSize,
                "page"));

        Assert.Equal("claim", Assert.Throws<ArgumentOutOfRangeException>(() =>
            DurableSendOperationLimits.ValidateClaimCount(0, "claim")).ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => DurableSendOperationLimits.ValidateClaimCount(
            DurableSendOperationLimits.AbsoluteMaximumClaimCount + 1,
            "claim"));
        Assert.Equal("page", Assert.Throws<ArgumentOutOfRangeException>(() =>
            DurableSendOperationLimits.ValidateQuarantinePageSize(0, "page")).ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => DurableSendOperationLimits.ValidateQuarantinePageSize(
            DurableSendOperationLimits.AbsoluteMaximumQuarantinePageSize + 1,
            "page"));
        Assert.Equal("parameterName", Assert.Throws<ArgumentNullException>(() =>
            DurableSendOperationLimits.ValidateClaimCount(1, null!)).ParamName);
        Assert.Equal("pageSize", Assert.Throws<ArgumentOutOfRangeException>(() =>
            DurableSendQuarantineQuery.FirstPage(0)).ParamName);
        Assert.Equal("pageSize", Assert.Throws<ArgumentOutOfRangeException>(() =>
            ReliableInboxQuarantineQuery.FirstPage(0)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-SERIALIZATION", "exact-content-address-and-zero-body-boundaries")]
    public void SerializedIntent_AcceptsExactTextBoundsAndAZeroByteBody()
    {
        Uri address = AddressWithAbsoluteLength(SerializedDurableSend.MaximumDestinationAddressCharacters);
        var message = Message() with
        {
            DestinationAddress = address,
            ContentType = ContentTypeWithLength(SerializedDurableSend.MaximumContentTypeCharacters),
            Body = ReadOnlyMemory<byte>.Empty,
            Metadata = ReadOnlyMemory<byte>.Empty,
        };

        Assert.Same(message, message.Validate());
        Assert.Equal(SerializedDurableSend.MaximumDestinationAddressCharacters, address.AbsoluteUri.Length);
        Assert.Equal(0, message.StorageSize);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-SERIALIZATION", "invalid-identity-address-and-content-rejected")]
    public void SerializedIntent_RejectsEveryInvalidPersistedFieldBeforeStorage()
    {
        SerializedDurableSend valid = Message();

        Assert.Equal("Id", Assert.Throws<ArgumentException>(() => (valid with { Id = default }).Validate()).ParamName);
        Assert.Equal("ContractIdentity", Assert.Throws<ArgumentException>(() =>
            (valid with { ContractIdentity = default }).Validate()).ParamName);
        Assert.Equal("DestinationAddress", Assert.Throws<ArgumentException>(() =>
            (valid with { DestinationAddress = new Uri("relative", UriKind.Relative) }).Validate()).ParamName);
        Assert.Equal("DestinationAddress", Assert.Throws<ArgumentException>(() =>
            (valid with { DestinationAddress = null! }).Validate()).ParamName);
        Assert.Equal("DestinationAddress", Assert.Throws<ArgumentOutOfRangeException>(() =>
            (valid with
            {
                DestinationAddress = AddressWithAbsoluteLength(
                SerializedDurableSend.MaximumDestinationAddressCharacters + 1)
            }).Validate()).ParamName);
        Assert.Equal("ContentType", Assert.Throws<ArgumentException>(() =>
            (valid with { ContentType = " " }).Validate()).ParamName);
        Assert.Equal("ContentType", Assert.Throws<ArgumentException>(() =>
            (valid with { ContentType = "application/json\r\nInjected: true" }).Validate()).ParamName);
        Assert.Equal("ContentType", Assert.Throws<ArgumentException>(() =>
            (valid with { ContentType = "not-a-media-type" }).Validate()).ParamName);
        Assert.Equal("ContentType", Assert.Throws<ArgumentOutOfRangeException>(() =>
            (valid with { ContentType = new string('a', SerializedDurableSend.MaximumContentTypeCharacters + 1) })
            .Validate()).ParamName);
        Assert.Equal("MessageId", Assert.Throws<ArgumentException>(() =>
            (valid with { MessageId = Guid.Empty }).Validate()).ParamName);
        Assert.Equal("CorrelationId", Assert.Throws<ArgumentException>(() =>
            (valid with { CorrelationId = Guid.Empty }).Validate()).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-CAPACITY", "body-plus-servicebus-metadata-ledger")]
    public void StorageSize_CountsOnlyBodyAndServiceBusMetadataBytes()
    {
        SerializedDurableSend message = Message() with
        {
            Body = new byte[13],
            Metadata = new byte[7],
            ContentType = ContentTypeWithLength(200),
            DestinationAddress = new Uri("https://example.test/a/long/transport/address"),
        };

        Assert.Equal(20, message.StorageSize);
        Assert.Same(message, message.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-ADMISSION", "new-versus-idempotent-dispositions")]
    public void AdmissionResult_IdentifiesOnlyANewlyCommittedIntentAsNew()
    {
        DurableSendId id = Message().Id;

        Assert.True(new DurableSendAdmissionResult(id, DurableSendAdmissionDisposition.Accepted, 1, 2).IsNew);
        Assert.False(new DurableSendAdmissionResult(id, DurableSendAdmissionDisposition.AlreadyAccepted, 1, 2).IsNew);
        Assert.False(new DurableSendAdmissionResult(id, DurableSendAdmissionDisposition.AlreadyQuarantined, 1, 2).IsNew);
        Assert.Equal(DurableSendCompletionMode.TransportAcceptance,
            DurableSendDispatchResult.TransportAccepted.CompletionMode);
        Assert.Equal(DurableSendCompletionMode.ConsumerCompletion,
            DurableSendDispatchResult.AwaitConsumerCompletion.CompletionMode);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-QUARANTINE", "operator-evidence-is-payload-free")]
    public void QuarantineEvidence_ExposesNoPayloadOrMetadataProperty()
    {
        string[] propertyNames = typeof(DurableSendQuarantineEntry).GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain("Body", propertyNames);
        Assert.DoesNotContain("Metadata", propertyNames);
        Assert.Contains(nameof(DurableSendQuarantineEntry.FailureKind), propertyNames);
        Assert.Contains(nameof(DurableSendQuarantineEntry.ContractIdentity), propertyNames);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-QUARANTINE", "valid-terminal-evidence-page-boundary")]
    public void DurableQuarantinePage_AcceptsAnAttemptAtTheAdmissionTimestamp()
    {
        DurableSendQuarantineEntry valid = QuarantineEntry();
        DurableSendQuarantineEntry entry = valid with
        {
            QuarantinedAt = valid.EnqueuedAt,
            DeliveryAttempts = 1,
            FailureType = null,
        };

        DurableSendQuarantinePage page = DurableSendQuarantinePagination.CreatePage([entry], 1);

        Assert.Same(entry, Assert.Single(page.Entries));
        Assert.False(page.HasMore);
        Assert.Null(page.NextQuery);
        Assert.Throws<NotSupportedException>(() => ((IList<DurableSendQuarantineEntry>)page.Entries).Clear());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-QUARANTINE", "reject-corrupt-persisted-identity-and-destination")]
    public void DurableQuarantinePage_RejectsCorruptIdentityAndDestinationBeforeExposure()
    {
        DurableSendQuarantineEntry valid = QuarantineEntry();

        Assert.Equal("Id", Assert.Throws<ArgumentException>(() =>
            DurableSendQuarantinePagination.CreatePage([valid with { Id = default }], 1)).ParamName);
        Assert.Equal("ContractIdentity", Assert.Throws<ArgumentException>(() =>
            DurableSendQuarantinePagination.CreatePage([valid with { ContractIdentity = default }], 1)).ParamName);
        Assert.Equal("DestinationAddress", Assert.Throws<ArgumentException>(() =>
            DurableSendQuarantinePagination.CreatePage([valid with { DestinationAddress = null! }], 1)).ParamName);
        Assert.Equal("DestinationAddress", Assert.Throws<ArgumentException>(() =>
            DurableSendQuarantinePagination.CreatePage(
                [valid with { DestinationAddress = new Uri("relative", UriKind.Relative) }], 1)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-QUARANTINE", "reject-null-page-evidence")]
    public void DurableQuarantinePage_RejectsANullEntryAtThePublicBoundary()
    {
        Assert.Equal("fetchedEntries", Assert.Throws<ArgumentException>(() =>
            DurableSendQuarantinePagination.CreatePage([null!], 1)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-QUARANTINE", "reject-impossible-terminal-evidence")]
    public void DurableQuarantinePage_RejectsImpossibleTerminalStateBeforeExposure()
    {
        DurableSendQuarantineEntry valid = QuarantineEntry();

        Assert.Equal("QuarantinedAt", Assert.Throws<ArgumentException>(() =>
            DurableSendQuarantinePagination.CreatePage(
                [valid with { QuarantinedAt = valid.EnqueuedAt.AddTicks(-1) }], 1)).ParamName);
        Assert.Equal("FailureKind", Assert.Throws<ArgumentException>(() =>
            DurableSendQuarantinePagination.CreatePage(
                [valid with { FailureKind = DurableSendFailureKind.None }], 1)).ParamName);
        Assert.Equal("FailureKind", Assert.Throws<ArgumentException>(() =>
            DurableSendQuarantinePagination.CreatePage(
                [valid with { FailureKind = (DurableSendFailureKind)int.MaxValue }], 1)).ParamName);
        Assert.Equal("FailureType", Assert.Throws<ArgumentException>(() =>
            DurableSendQuarantinePagination.CreatePage([valid with { FailureType = " \t" }], 1)).ParamName);
        Assert.Equal("DeliveryAttempts", Assert.Throws<ArgumentOutOfRangeException>(() =>
            DurableSendQuarantinePagination.CreatePage([valid with { DeliveryAttempts = 0 }], 1)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-ADMISSION", "provider-result-invariants")]
    public void ProviderResults_RejectInvalidIdentitiesCountsAndCompletionBoundaries()
    {
        SerializedDurableSend message = Message();
        var completion = new ConsumerCompletion(message.Id);
        var dispatch = new DurableSendDispatchContext(message, message.Id, 1, completion);
        var delivery = new DurableSendDelivery
        {
            Message = message,
            GenerationToken = Guid.NewGuid(),
            EnqueuedAt = DateTimeOffset.Parse("2026-09-03T12:00:00+00:00"),
            DeliveryAttempts = 0,
            Status = DurableSendStatus.Pending,
            Lease = new DurableSendLease(Guid.NewGuid(), DateTimeOffset.Parse("2026-09-03T12:01:00+00:00")),
        };

        Assert.Equal(message.Id, dispatch.DurableSendId);
        Assert.Equal(1, dispatch.Attempt);
        Assert.Same(delivery, delivery.Validate());
        Assert.Equal("id", Assert.Throws<ArgumentException>(() => new DurableSendAdmissionResult(
            default,
            DurableSendAdmissionDisposition.Accepted,
            0,
            0)).ParamName);
        Assert.Equal("disposition", Assert.Throws<ArgumentException>(() => new DurableSendAdmissionResult(
            message.Id,
            (DurableSendAdmissionDisposition)999,
            0,
            0)).ParamName);
        Assert.Equal("storedCount", Assert.Throws<ArgumentOutOfRangeException>(() => new DurableSendAdmissionResult(
            message.Id,
            DurableSendAdmissionDisposition.Accepted,
            -1,
            0)).ParamName);
        Assert.Equal("id", Assert.Throws<ArgumentException>(() => new DurableSendReceipt(
            default,
            DurableSendAdmissionDisposition.Accepted,
            0,
            0)).ParamName);
        Assert.Equal("completionMode", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DurableSendDispatchResult((DurableSendCompletionMode)999)).ParamName);
        Assert.Equal("completionMode", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DurableSendDispatchResult(DurableSendCompletionMode.Unknown)).ParamName);
        Assert.Equal("attempt", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DurableSendDispatchContext(message, message.Id, 0, completion)).ParamName);
        Assert.Equal("durableSendId", Assert.Throws<ArgumentException>(() =>
            new DurableSendDispatchContext(message, new DurableSendId(Guid.NewGuid()), 1, completion)).ParamName);
        Assert.Equal("consumerCompletion", Assert.Throws<ArgumentException>(() =>
            new DurableSendDispatchContext(message, message.Id, 1, new ConsumerCompletion(new DurableSendId(Guid.NewGuid())))).ParamName);
        Assert.Equal("Status", Assert.Throws<ArgumentException>(() =>
            (delivery with { Status = DurableSendStatus.Quarantined }).Validate()).ParamName);
        Assert.Equal("GenerationToken", Assert.Throws<ArgumentException>(() =>
            (delivery with { GenerationToken = Guid.Empty }).Validate()).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-CAPACITY", "store-snapshot-state-invariants")]
    public void StoreSnapshot_RequiresConsistentNonnegativeStateAggregates()
    {
        DateTimeOffset oldest = DateTimeOffset.Parse("2026-09-03T12:00:00+00:00");
        var snapshot = new DurableSendStoreSnapshot(5, 100, 3, 1, 1, 2, oldest);

        Assert.Equal(5, snapshot.StoredCount);
        Assert.Equal(3, snapshot.PendingCount);
        Assert.Equal(2, snapshot.QuarantinedCount);
        Assert.Equal("storedCount", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DurableSendStoreSnapshot(-1, 0, 0, 0, 0, 0, null)).ParamName);
        Assert.Throws<ArgumentException>(() => new DurableSendStoreSnapshot(4, 0, 3, 0, 0, 2, oldest));
        Assert.Throws<ArgumentException>(() => new DurableSendStoreSnapshot(3, 0, 3, 2, 2, 0, oldest));
        Assert.Equal("oldestPendingEnqueuedAt", Assert.Throws<ArgumentException>(() =>
            new DurableSendStoreSnapshot(0, 0, 0, 0, 0, 0, oldest)).ParamName);
        Assert.Equal("oldestPendingEnqueuedAt", Assert.Throws<ArgumentException>(() =>
            new DurableSendStoreSnapshot(1, 0, 1, 0, 0, 0, null)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "identity-acquisition-and-reference-invariants")]
    public void InboxAcquisitionAndReferences_RejectImpossibleIdentityAndLeaseCombinations()
    {
        var key = new ReliableInboxKey(Guid.NewGuid(), Guid.NewGuid());
        var lease = new ReliableInboxLease(Guid.NewGuid(), DateTimeOffset.Parse("2026-09-03T12:00:00+00:00"));

        var acquired = new ReliableInboxAcquireResult(key, ReliableInboxAcquireDisposition.Acquired, lease, 1);
        var consumed = new ReliableInboxAcquireResult(key, ReliableInboxAcquireDisposition.AlreadyConsumed, null, 1);
        ReliableMessageReference outboxReference = ReliableMessageReference.Outbox(Message().Id);
        var notFound = new ReliableMessagingOperationResult(
            outboxReference,
            ReliableMessagingOperationDisposition.NotFound,
            null,
            null);

        Assert.Equal(lease, acquired.Lease);
        Assert.Null(consumed.Lease);
        Assert.False(notFound.IsApplied);
        Assert.Equal("MessageId", Assert.Throws<ArgumentException>(() => new ReliableInboxKey(default, key.ConsumerId).Validate()).ParamName);
        Assert.Equal("id", Assert.Throws<ArgumentException>(() => ReliableMessageReference.Outbox(default)).ParamName);
        Assert.Equal("MessageId", Assert.Throws<ArgumentException>(() => ReliableMessageReference.Inbox(default)).ParamName);
        Assert.Equal("lease", Assert.Throws<ArgumentException>(() => new ReliableInboxAcquireResult(
            key,
            ReliableInboxAcquireDisposition.Acquired,
            null,
            1)).ParamName);
        Assert.Equal("lease", Assert.Throws<ArgumentException>(() => new ReliableInboxAcquireResult(
            key,
            ReliableInboxAcquireDisposition.Busy,
            lease,
            1)).ParamName);
        Assert.Equal("attempt", Assert.Throws<ArgumentOutOfRangeException>(() => new ReliableInboxAcquireResult(
            key,
            ReliableInboxAcquireDisposition.AlreadyConsumed,
            null,
            0)).ParamName);
        Assert.Equal("Kind", Assert.Throws<ArgumentException>(() => default(ReliableMessageReference).Validate()).ParamName);
        Assert.Equal("disposition", Assert.Throws<ArgumentException>(() => new ReliableMessagingOperationResult(
            outboxReference,
            (ReliableMessagingOperationDisposition)999,
            null,
            null)).ParamName);
        Assert.Equal("currentState", Assert.Throws<ArgumentException>(() => new ReliableMessagingOperationResult(
            outboxReference,
            ReliableMessagingOperationDisposition.NotFound,
            null,
            "Pending")).ParamName);
        Assert.Equal("previousState", Assert.Throws<ArgumentException>(() => new ReliableMessagingOperationResult(
            outboxReference,
            ReliableMessagingOperationDisposition.Applied,
            " ",
            "Pending")).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-QUARANTINE", "immutable-validated-operation-pages")]
    public void QuarantinePages_SnapshotEntriesAndRejectInvalidEvidence()
    {
        DateTimeOffset receivedAt = DateTimeOffset.Parse("2026-09-03T12:00:00+00:00");
        var key = new ReliableInboxKey(Guid.NewGuid(), Guid.NewGuid());
        var entry = new ReliableInboxQuarantineEntry(
            key,
            ReliableInboxStatus.Quarantined,
            2,
            receivedAt,
            receivedAt.AddMinutes(1),
            typeof(InvalidOperationException).FullName);
        var mutableEntries = new List<ReliableInboxQuarantineEntry> { entry };
        var page = new ReliableInboxQuarantinePage(mutableEntries, null);

        mutableEntries.Clear();

        Assert.Single(page.Entries);
        Assert.False(page.HasMore);
        Assert.Throws<NotSupportedException>(() => ((IList<ReliableInboxQuarantineEntry>)page.Entries).Clear());
        Assert.Equal("Status", Assert.Throws<ArgumentException>(() => new ReliableInboxQuarantinePage(
            [entry with { Status = ReliableInboxStatus.Processing }],
            null)).ParamName);
        Assert.Equal("QuarantinedAt", Assert.Throws<ArgumentException>(() => new ReliableInboxQuarantinePage(
            [entry with { QuarantinedAt = receivedAt.AddTicks(-1) }],
            null)).ParamName);
    }

    private static SerializedDurableSend Message() => new()
    {
        Id = new DurableSendId(Guid.Parse("77777777-2222-3333-4444-555555555555")),
        ContractIdentity = new MessageContractIdentity("vicione.tests.durable", 1),
        DestinationAddress = new Uri("https://example.test/durable"),
        ContentType = "application/octet-stream",
        Body = new byte[] { 1 },
    };

    private static DurableSendQuarantineEntry QuarantineEntry()
    {
        DateTimeOffset enqueuedAt = DateTimeOffset.Parse("2026-09-03T12:00:00+00:00");
        return new DurableSendQuarantineEntry
        {
            Id = new DurableSendId(Guid.Parse("88888888-2222-3333-4444-555555555555")),
            ContractIdentity = new MessageContractIdentity("vicione.tests.durable", 1),
            DestinationAddress = new Uri("https://example.test/durable"),
            EnqueuedAt = enqueuedAt,
            QuarantinedAt = enqueuedAt.AddMinutes(1),
            DeliveryAttempts = 2,
            FailureKind = DurableSendFailureKind.RetryLimitExceeded,
            FailureType = typeof(InvalidOperationException).FullName,
        };
    }

    private static Uri AddressWithAbsoluteLength(int length)
    {
        const string prefix = "https://example.test/";
        return new Uri(prefix + new string('a', length - prefix.Length));
    }

    private static string ContentTypeWithLength(int length)
    {
        const string prefix = "application/";
        return prefix + new string('a', length - prefix.Length);
    }

    private sealed class ConsumerCompletion(DurableSendId durableSendId) : IDurableSendConsumerCompletion
    {
        public DurableSendId DurableSendId { get; } = durableSendId;

        public ValueTask<bool> CompleteAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(true);
    }
}
