namespace ViciOne.ServiceBus.MessageJournal;

/// <summary>
/// Identifies the terminal outcome observed by the journal.
/// </summary>
public enum MessageJournalOutcome
{
    /// <summary>
    /// Indicates succeeded.
    /// </summary>
    Succeeded = 1,
    /// <summary>
    /// Indicates faulted.
    /// </summary>
    Faulted = 2,
}
