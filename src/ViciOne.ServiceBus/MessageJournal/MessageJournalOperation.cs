namespace ViciOne.ServiceBus.MessageJournal;

/// <summary>Identifies the completed message operation observed by the journal.</summary>
public enum MessageJournalOperation
{
    /// <summary>A message was sent to one destination.</summary>
    Send = 1,
    /// <summary>A message was published to its subscribers.</summary>
    Publish = 2,
    /// <summary>A message was processed by a consumer pipeline.</summary>
    Consume = 3,
}
