namespace ViciOne.ServiceBus.Operations;

/// <summary>Disposition of an explicit reliable-messaging operator action.</summary>
public enum ReliableMessagingOperationDisposition
{
    /// <summary>The requested state transition was applied.</summary>
    Applied = 0,

    /// <summary>The referenced record does not exist.</summary>
    NotFound = 1,

    /// <summary>The record exists but its current state does not permit the requested transition.</summary>
    InvalidState = 2,
}
