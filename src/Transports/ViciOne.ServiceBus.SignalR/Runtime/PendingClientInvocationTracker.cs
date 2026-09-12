using System.Collections.Concurrent;

namespace ViciOne.ServiceBus.SignalR.Runtime;

/// <summary>Tracks local result waiters and remote invocations until SignalR reports their completion.</summary>
internal sealed class PendingClientInvocationTracker
{
    readonly ConcurrentDictionary<string, PendingClientInvocation> _invocations = new(StringComparer.Ordinal);

    /// <summary>Gets the number of invocations that still require completion or cancellation.</summary>
    public int Count => _invocations.Count;

    /// <summary>Adds a result waiter owned by this node.</summary>
    public PendingClientInvocation AddLocal(string invocationId, string connectionId, Type resultType)
    {
        var invocation = PendingClientInvocation.CreateLocal(connectionId, resultType);
        if (!_invocations.TryAdd(invocationId, invocation))
            throw new InvalidOperationException($"SignalR invocation '{invocationId}' is already pending.");

        return invocation;
    }

    /// <summary>Adds the forwarding state required for a result requested by another node.</summary>
    public bool TryAddRemote(
        string invocationId,
        string connectionId,
        string resultNodeId,
        string protocolName)
    {
        return _invocations.TryAdd(
            invocationId,
            PendingClientInvocation.CreateRemote(connectionId, resultNodeId, protocolName));
    }

    /// <summary>Gets a pending invocation without changing its ownership.</summary>
    public bool TryGet(string invocationId, out PendingClientInvocation? invocation)
    {
        return _invocations.TryGetValue(invocationId, out invocation);
    }

    /// <summary>Removes a pending invocation only when the supplied connection still owns it.</summary>
    public bool TryRemove(
        string invocationId,
        string connectionId,
        out PendingClientInvocation? invocation)
    {
        invocation = null;
        if (!_invocations.TryGetValue(invocationId, out PendingClientInvocation? candidate) ||
            !string.Equals(candidate.ConnectionId, connectionId, StringComparison.Ordinal))
        {
            return false;
        }

        bool removed = ((ICollection<KeyValuePair<string, PendingClientInvocation>>)_invocations)
            .Remove(new KeyValuePair<string, PendingClientInvocation>(invocationId, candidate));
        invocation = removed ? candidate : null;
        return removed;
    }

    /// <summary>Removes an invocation when the caller still holds the same tracker entry.</summary>
    public bool TryRemove(string invocationId, PendingClientInvocation invocation)
    {
        return ((ICollection<KeyValuePair<string, PendingClientInvocation>>)_invocations)
            .Remove(new KeyValuePair<string, PendingClientInvocation>(invocationId, invocation));
    }

    /// <summary>Removes every invocation owned by a disconnected connection.</summary>
    public RemovedPendingClientInvocation[] RemoveForConnection(string connectionId)
    {
        var removed = new List<RemovedPendingClientInvocation>();
        foreach ((string invocationId, PendingClientInvocation invocation) in _invocations)
        {
            if (string.Equals(invocation.ConnectionId, connectionId, StringComparison.Ordinal) &&
                ((ICollection<KeyValuePair<string, PendingClientInvocation>>)_invocations)
                    .Remove(new KeyValuePair<string, PendingClientInvocation>(invocationId, invocation)))
            {
                removed.Add(new RemovedPendingClientInvocation(invocationId, invocation));
            }
        }

        return [.. removed];
    }

    /// <summary>Gets the result type needed by a hub protocol to parse a client completion.</summary>
    public bool TryGetResultType(string invocationId, out Type? resultType)
    {
        if (_invocations.TryGetValue(invocationId, out PendingClientInvocation? invocation))
        {
            resultType = invocation.ResultType;
            return true;
        }

        resultType = null;
        return false;
    }
}
