namespace ViciOne.ServiceBus.MessageJournal;

/// <summary>
/// Identifies the completed message operation observed by the journal.
/// </summary>
public enum MessageJournalOperation
{
    /// <summary>
    /// Indicates send.
    /// </summary>
    Send = 1,
    /// <summary>
    /// Indicates publish.
    /// </summary>
    Publish = 2,
    /// <summary>
    /// Indicates consume.
    /// </summary>
    Consume = 3,
}
