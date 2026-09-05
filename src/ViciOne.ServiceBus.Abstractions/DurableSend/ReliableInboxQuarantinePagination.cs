using ViciOne.ServiceBus.Operations;

namespace ViciOne.ServiceBus.Providers.Persistence;

internal static class ReliableInboxQuarantinePagination
{
    public static ReliableInboxQuarantineQuery Validate(ReliableInboxQuarantineQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        DurableSendOperationLimits.ValidateQuarantinePageSize(query.PageSize, nameof(query.PageSize));

        bool anyCursor = query.AfterQuarantinedAt.HasValue
            || query.AfterMessageId.HasValue
            || query.AfterConsumerId.HasValue;
        bool completeCursor = query.AfterQuarantinedAt.HasValue
            && query.AfterMessageId.HasValue
            && query.AfterConsumerId.HasValue;
        if (anyCursor && !completeCursor)
        {
            throw new ArgumentException(
                "All inbox quarantine cursor values must be supplied together.",
                nameof(query));
        }

        return query;
    }
}
