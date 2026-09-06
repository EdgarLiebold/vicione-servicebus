namespace ViciOne.ServiceBus.MessageJournal;

/// <summary>
/// Declares the handling classification of sanitized journal content.
/// </summary>
public enum MessageJournalDataClassification
{
    /// <summary>
    /// Indicates public.
    /// </summary>
    Public = 1,
    /// <summary>
    /// Indicates internal.
    /// </summary>
    Internal = 2,
    /// <summary>
    /// Indicates confidential.
    /// </summary>
    Confidential = 3,
    /// <summary>
    /// Indicates restricted.
    /// </summary>
    Restricted = 4,
}
