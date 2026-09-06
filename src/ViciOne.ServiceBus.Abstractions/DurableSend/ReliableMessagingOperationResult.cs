namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Typed, idempotent outcome returned by reliable-messaging operator actions.</summary>
public sealed record ReliableMessagingOperationResult(
    ReliableMessageReference Reference,
    ReliableMessagingOperationDisposition Disposition,
    string? PreviousState,
    string? CurrentState);
