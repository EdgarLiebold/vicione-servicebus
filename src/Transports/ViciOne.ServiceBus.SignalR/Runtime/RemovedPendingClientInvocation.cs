namespace ViciOne.ServiceBus.SignalR.Runtime;

/// <summary>Pairs a removed invocation with the identifier needed for cleanup signaling.</summary>
internal sealed record RemovedPendingClientInvocation(
    string InvocationId,
    PendingClientInvocation Invocation);
