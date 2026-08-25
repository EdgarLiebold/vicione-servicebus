#nullable enable
namespace ViciOne.ServiceBus.MessageJournal;

/// <summary>
/// Identifies the terminal outcome observed by the journal.
/// </summary>
public enum MessageJournalOutcome
{
    Succeeded = 1,
    Faulted = 2,
}
