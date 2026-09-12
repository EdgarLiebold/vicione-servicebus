using Microsoft.AspNetCore.SignalR;

namespace ViciOne.ServiceBus.SignalR.Contracts;

/// <summary>Routes cancellation of a pending client invocation to its owning connection.</summary>
internal sealed class InvocationCancellationMessage<THub>
    where THub : Hub
{
    /// <summary>Initializes a cancellation for an exact invocation and connection.</summary>
    public InvocationCancellationMessage(string connectionId, string invocationId)
    {
        ConnectionId = connectionId ?? throw new ArgumentNullException(nameof(connectionId));
        InvocationId = invocationId ?? throw new ArgumentNullException(nameof(invocationId));
    }

    /// <summary>Gets the connection that received the invocation.</summary>
    public string ConnectionId { get; }

    /// <summary>Gets the invocation to cancel.</summary>
    public string InvocationId { get; }
}
