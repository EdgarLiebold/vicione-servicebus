using ViciOne.ServiceBus.MessageJournal;

namespace ViciOne.ServiceBus.Tests.InternalAccess.MessageJournal;
/// <summary>Creates immutable runtime entries without widening the product constructor.</summary>
public static class MessageJournalEntryTestFactory
{
    public static MessageJournalEntry Create(
        Guid entryId,
        DateTimeOffset observedAt,
        MessageJournalOperation operation = MessageJournalOperation.Send,
        MessageJournalOutcome outcome = MessageJournalOutcome.Succeeded,
        MessageJournalDataClassification dataClassification = MessageJournalDataClassification.Internal,
        string? contentType = "application/json",
        IEnumerable<string>? messageTypes = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        IReadOnlyDictionary<string, string>? headers = null,
        ReadOnlyMemory<byte> body = default)
    {
        var projection = new MessageJournalProjection(
            dataClassification,
            contentType,
            messageTypes,
            metadata,
            headers,
            body);

        return new MessageJournalEntry(entryId, observedAt, operation, outcome, projection);
    }
}
