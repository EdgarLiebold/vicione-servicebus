namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Outcome of trying to acquire one inbox identity.</summary>
public enum ReliableInboxAcquireDisposition
{
    /// <summary>The caller owns a new fenced processing attempt.</summary>
    Acquired = 0,

    /// <summary>The same message was already consumed and must not execute again.</summary>
    AlreadyConsumed = 1,

    /// <summary>Another non-expired lease owns the message.</summary>
    Busy = 2,

    /// <summary>The message is quarantined or abandoned.</summary>
    Unavailable = 3,

    /// <summary>The retry exists but is not due yet.</summary>
    NotDue = 4,
}
