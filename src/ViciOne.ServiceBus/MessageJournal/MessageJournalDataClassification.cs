namespace ViciOne.ServiceBus.MessageJournal;

/// <summary>Declares the handling classification of sanitized journal content.</summary>
public enum MessageJournalDataClassification
{
    /// <summary>Content approved for unrestricted disclosure.</summary>
    Public = 1,
    /// <summary>Content restricted to normal internal operational access.</summary>
    Internal = 2,
    /// <summary>Sensitive content requiring explicitly authorized access.</summary>
    Confidential = 3,
    /// <summary>Highly sensitive content subject to the strongest configured handling controls.</summary>
    Restricted = 4,
}
