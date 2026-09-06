namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Typed, idempotent outcome returned by reliable-messaging operator actions.</summary>
/// <param name="Reference">The reference.</param>
/// <param name="Disposition">The disposition.</param>
/// <param name="PreviousState">The previous state.</param>
/// <param name="CurrentState">The current state.</param>
public sealed record ReliableMessagingOperationResult(
    ReliableMessageReference Reference,
    ReliableMessagingOperationDisposition Disposition,
    string? PreviousState,
    string? CurrentState);
