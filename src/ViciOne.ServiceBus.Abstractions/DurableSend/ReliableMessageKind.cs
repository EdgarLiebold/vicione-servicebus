namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Identifies which side of reliable messaging owns an operator reference.</summary>
public enum ReliableMessageKind
{
    /// <summary>The reference identifies an outbox intent.</summary>
    Outbox = 0,

    /// <summary>The reference identifies an inbox processing record.</summary>
    Inbox = 1,
}
