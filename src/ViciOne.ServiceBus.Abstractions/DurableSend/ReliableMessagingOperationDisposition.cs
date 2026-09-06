namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Disposition of an explicit reliable-messaging operator action.</summary>
public enum ReliableMessagingOperationDisposition
{
    /// <summary>The requested state transition was applied.</summary>
    Applied = 0,

    /// <summary>The referenced record does not exist.</summary>
    NotFound = 1,

    /// <summary>The record exists but is not in a compatible state.</summary>
    InvalidState = 2,
}
