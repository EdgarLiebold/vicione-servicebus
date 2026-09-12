using Microsoft.AspNetCore.SignalR.Protocol;

namespace ViciOne.ServiceBus.SignalR.Runtime;

/// <summary>Describes either a local result waiter or a remote result-forwarding obligation.</summary>
internal sealed class PendingClientInvocation
{
    PendingClientInvocation(
        string connectionId,
        Type resultType,
        TaskCompletionSource<CompletionMessage>? completion,
        string? resultNodeId,
        string? protocolName)
    {
        ConnectionId = connectionId;
        ResultType = resultType;
        Completion = completion;
        ResultNodeId = resultNodeId;
        ProtocolName = protocolName;
    }

    /// <summary>Gets the connection expected to produce the result.</summary>
    public string ConnectionId { get; }

    /// <summary>Gets the type used to parse the completion payload.</summary>
    public Type ResultType { get; }

    /// <summary>Gets the local waiter, or <see langword="null" /> when another node owns it.</summary>
    public TaskCompletionSource<CompletionMessage>? Completion { get; }

    /// <summary>Gets the remote node awaiting this result.</summary>
    public string? ResultNodeId { get; }

    /// <summary>Gets the owning connection's hub protocol for remote result forwarding.</summary>
    public string? ProtocolName { get; }

    /// <summary>Gets whether the invocation result must be forwarded to another node.</summary>
    public bool IsRemote => ResultNodeId is not null;

    /// <summary>Creates a local waiter with asynchronous continuation dispatch.</summary>
    public static PendingClientInvocation CreateLocal(string connectionId, Type resultType)
    {
        return new PendingClientInvocation(
            connectionId,
            resultType,
            new TaskCompletionSource<CompletionMessage>(TaskCreationOptions.RunContinuationsAsynchronously),
            null,
            null);
    }

    /// <summary>Creates forwarding state that parses the client result without materializing its application type.</summary>
    public static PendingClientInvocation CreateRemote(
        string connectionId,
        string resultNodeId,
        string protocolName)
    {
        return new PendingClientInvocation(
            connectionId,
            typeof(RawResult),
            null,
            resultNodeId,
            protocolName);
    }
}
