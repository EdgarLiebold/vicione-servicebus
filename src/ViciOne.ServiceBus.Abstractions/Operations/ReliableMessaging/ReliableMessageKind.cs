namespace ViciOne.ServiceBus.Operations;

/// <summary>Identifies which side of reliable messaging owns an operator reference.</summary>
public enum ReliableMessageKind
{
    /// <summary>The reference has not been initialized.</summary>
    Unknown = 0,

    /// <summary>The reference identifies an outbox intent.</summary>
    Outbox = 1,

    /// <summary>The reference identifies an inbox processing record.</summary>
    Inbox = 2,
}
