using ViciOne.ServiceBus.Operations;

namespace ViciOne.ServiceBus.Providers.Persistence;

internal static class ReliableMessagingProviderGuard
{
    public static DurableSendAdmissionResult ValidateAdmission(
        SerializedDurableSend message,
        DurableSendAdmissionResult result)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (result.Id != message.Id
            || !Enum.IsDefined(result.Disposition)
            || result.StoredCount < 1
            || result.StoredBytes < message.StorageSize)
        {
            throw InvalidResult("admission");
        }

        return result;
    }

    public static IReadOnlyList<DurableSendDelivery> ValidateClaims(
        IReadOnlyList<DurableSendDelivery>? deliveries,
        DateTimeOffset claimedAt,
        int maximumCount)
    {
        if (deliveries is null || deliveries.Count > maximumCount)
            throw InvalidResult("claim");

        var identities = new HashSet<DurableSendId>();
        var snapshot = new DurableSendDelivery[deliveries.Count];
        for (var index = 0; index < deliveries.Count; index++)
        {
            DurableSendDelivery delivery;
            try
            {
                delivery = deliveries[index] ?? throw InvalidResult("claim");
                _ = delivery.Validate();
            }
            catch (ArgumentException exception)
            {
                throw InvalidResult("claim", exception);
            }

            if (delivery.Lease.ExpiresAt <= claimedAt
                || delivery.Status == DurableSendStatus.Pending && delivery.Message.DueAt > claimedAt
                || !identities.Add(delivery.Message.Id))
            {
                throw InvalidResult("claim");
            }

            snapshot[index] = delivery;
        }

        return snapshot;
    }

    public static ReliableInboxAcquireResult ValidateAcquisition(
        ReliableInboxKey key,
        ReliableInboxAcquireResult? result)
    {
        if (result is null
            || result.Key != key
            || !Enum.IsDefined(result.Disposition)
            || result.Attempt < 1
            || (result.Disposition == ReliableInboxAcquireDisposition.Acquired) != result.Lease.HasValue
            || result.Lease is { Token: var token } && token == Guid.Empty)
        {
            throw InvalidResult("inbox acquisition");
        }

        return result;
    }

    public static DurableSendOperationResult ValidateOutboxOperation(
        DurableSendId id,
        DurableSendOperationResult result)
    {
        if (result.Id != id || !Enum.IsDefined(result.Outcome))
            throw InvalidResult("outbox operation");

        return result;
    }

    public static ReliableMessagingOperationResult ValidateMessagingOperation(
        ReliableMessageReference reference,
        ReliableMessagingOperationResult? result)
    {
        if (result is null || result.Reference != reference)
            throw InvalidResult("operation");

        return result;
    }

    public static DurableSendQuarantinePage ValidateOutboxPage(
        DurableSendQuarantineQuery query,
        DurableSendQuarantinePage? page)
    {
        if (page is null
            || page.Entries.Count > query.PageSize
            || page.HasMore && page.Entries.Count != query.PageSize)
        {
            throw InvalidResult("outbox quarantine query");
        }

        return page;
    }

    public static ReliableInboxQuarantinePage ValidateInboxPage(
        ReliableInboxQuarantineQuery query,
        ReliableInboxQuarantinePage? page)
    {
        if (page is null
            || page.Entries.Count > query.PageSize
            || page.HasMore && page.Entries.Count != query.PageSize
            || page.Next is { } next && next.PageSize != query.PageSize)
        {
            throw InvalidResult("inbox quarantine query");
        }

        return page;
    }

    static InvalidOperationException InvalidResult(string operation, Exception? innerException = null) =>
        new($"The reliable-messaging persistence provider returned an invalid {operation} result.", innerException);
}
