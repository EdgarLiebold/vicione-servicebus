using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Represents the outcome an activity returns after execution.</summary>
public interface ExecutionResult
{
    /// <summary>Applies the execution outcome to the routing slip.</summary>
    /// <param name="cancellationToken">The token that cancels outcome processing.</param>
    /// <returns>A task that completes after the routing slip reflects the outcome.</returns>
    Task EvaluateAsync(CancellationToken cancellationToken = default);

    /// <summary>Determines whether activity execution faulted.</summary>
    /// <param name="exception">Receives the execution failure when one is present.</param>
    /// <returns><see langword="true" /> when execution faulted; otherwise, <see langword="false" />.</returns>
    bool IsFaulted([NotNullWhen(true)] out Exception? exception);
}
