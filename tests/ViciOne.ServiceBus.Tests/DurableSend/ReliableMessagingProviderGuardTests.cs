using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DurableSend;

public sealed class ReliableMessagingProviderGuardTests
{
    static readonly DateTimeOffset Epoch = DateTimeOffset.Parse("2026-09-03T12:00:00+00:00");

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-PROVIDER-CONTRACT", "results-are-bound-to-requested-identities")]
    public void IdentityBoundResults_RejectDefaultsMismatchesAndMissingInstances()
    {
        var outboxId = new DurableSendId(GuidFrom(1));
        var otherOutboxId = new DurableSendId(GuidFrom(2));
        var inboxKey = new ReliableInboxKey(GuidFrom(3), GuidFrom(4));
        var otherInboxKey = new ReliableInboxKey(GuidFrom(5), GuidFrom(6));
        ReliableMessageReference inboxReference = ReliableMessageReference.Inbox(inboxKey);

        Assert.Throws<InvalidOperationException>(() =>
            ReliableMessagingProviderGuard.ValidateAcquisition(inboxKey, null));
        Assert.Throws<InvalidOperationException>(() =>
            ReliableMessagingProviderGuard.ValidateAcquisition(
                inboxKey,
                new ReliableInboxAcquireResult(
                    otherInboxKey,
                    ReliableInboxAcquireDisposition.Acquired,
                    new ReliableInboxLease(GuidFrom(7), Epoch.AddMinutes(1)),
                    1)));
        Assert.Throws<InvalidOperationException>(() =>
            ReliableMessagingProviderGuard.ValidateOutboxOperation(outboxId, default));
        Assert.Throws<InvalidOperationException>(() =>
            ReliableMessagingProviderGuard.ValidateOutboxOperation(
                outboxId,
                new DurableSendOperationResult(otherOutboxId, DurableSendOperationOutcome.NotFound)));
        Assert.Throws<InvalidOperationException>(() =>
            ReliableMessagingProviderGuard.ValidateMessagingOperation(inboxReference, null));
        Assert.Throws<InvalidOperationException>(() =>
            ReliableMessagingProviderGuard.ValidateMessagingOperation(
                inboxReference,
                new ReliableMessagingOperationResult(
                    ReliableMessageReference.Inbox(otherInboxKey),
                    ReliableMessagingOperationDisposition.NotFound,
                    null,
                    null)));

        ReliableInboxAcquireResult acquisition = new(
            inboxKey,
            ReliableInboxAcquireDisposition.Acquired,
            new ReliableInboxLease(GuidFrom(8), Epoch.AddMinutes(1)),
            1);
        var operation = new ReliableMessagingOperationResult(
            inboxReference,
            ReliableMessagingOperationDisposition.NotFound,
            null,
            null);
        Assert.Same(acquisition, ReliableMessagingProviderGuard.ValidateAcquisition(inboxKey, acquisition));
        Assert.Same(operation, ReliableMessagingProviderGuard.ValidateMessagingOperation(inboxReference, operation));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-PROVIDER-CONTRACT", "quarantine-pages-remain-within-requested-bounds")]
    public void QuarantinePages_RejectMissingOversizedAndMalformedContinuations()
    {
        DurableSendQuarantineQuery outboxQuery = DurableSendQuarantineQuery.FirstPage(1);
        ReliableInboxQuarantineQuery inboxQuery = ReliableInboxQuarantineQuery.FirstPage(1);
        var inboxEntry = new ReliableInboxQuarantineEntry(
            new ReliableInboxKey(GuidFrom(10), GuidFrom(11)),
            ReliableInboxStatus.Quarantined,
            1,
            Epoch,
            Epoch.AddMinutes(1),
            "Tests.PermanentFailure");

        Assert.Throws<InvalidOperationException>(() =>
            ReliableMessagingProviderGuard.ValidateOutboxPage(outboxQuery, null));
        Assert.Throws<InvalidOperationException>(() =>
            ReliableMessagingProviderGuard.ValidateInboxPage(inboxQuery, null));
        Assert.Throws<InvalidOperationException>(() =>
            ReliableMessagingProviderGuard.ValidateInboxPage(
                inboxQuery,
                new ReliableInboxQuarantinePage([inboxEntry, inboxEntry], null)));
        Assert.Throws<InvalidOperationException>(() =>
            ReliableMessagingProviderGuard.ValidateInboxPage(
                inboxQuery,
                new ReliableInboxQuarantinePage(
                    [],
                    ReliableInboxQuarantineQuery.FirstPage(2))));

        DurableSendQuarantinePage outboxPage = DurableSendQuarantinePagination.CreatePage([], 1);
        var inboxPage = new ReliableInboxQuarantinePage([inboxEntry], null);
        Assert.Same(outboxPage, ReliableMessagingProviderGuard.ValidateOutboxPage(outboxQuery, outboxPage));
        Assert.Same(inboxPage, ReliableMessagingProviderGuard.ValidateInboxPage(inboxQuery, inboxPage));
    }

    static Guid GuidFrom(int value) => new(value, 0, 0, new byte[8]);
}
