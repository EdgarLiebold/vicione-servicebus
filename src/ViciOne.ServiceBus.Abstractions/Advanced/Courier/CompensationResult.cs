using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Represents the outcome an activity returns after compensation.</summary>
public interface CompensationResult
{
    /// <summary>Applies the compensation outcome to the routing slip.</summary>
    /// <param name="cancellationToken">The token that cancels outcome processing.</param>
    /// <returns>A task that completes after the routing slip reflects the outcome.</returns>
    Task EvaluateAsync(CancellationToken cancellationToken = default);

    /// <summary>Determines whether compensation failed.</summary>
    /// <param name="exception">Receives the compensation failure when one is present.</param>
    /// <returns><see langword="true" /> when compensation failed; otherwise, <see langword="false" />.</returns>
    bool IsFailed([NotNullWhen(true)] out Exception? exception);
}
