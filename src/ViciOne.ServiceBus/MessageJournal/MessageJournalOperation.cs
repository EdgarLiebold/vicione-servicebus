#nullable enable
namespace ViciOne.ServiceBus.MessageJournal;

/// <summary>
/// Identifies the completed message operation observed by the journal.
/// </summary>
public enum MessageJournalOperation
{
    Send = 1,
    Publish = 2,
    Consume = 3,
}
