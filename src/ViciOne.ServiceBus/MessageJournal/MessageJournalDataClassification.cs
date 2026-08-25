#nullable enable
namespace ViciOne.ServiceBus.MessageJournal;

/// <summary>
/// Declares the handling classification of sanitized journal content.
/// </summary>
public enum MessageJournalDataClassification
{
    Public = 1,
    Internal = 2,
    Confidential = 3,
    Restricted = 4,
}
