namespace ViciOne.ServiceBus.MessageJournal;

/// <summary>Identifies the terminal outcome observed by the journal.</summary>
public enum MessageJournalOutcome
{
    /// <summary>The observed operation completed successfully.</summary>
    Succeeded = 1,
    /// <summary>The observed operation completed with a fault.</summary>
    Faulted = 2,
}
