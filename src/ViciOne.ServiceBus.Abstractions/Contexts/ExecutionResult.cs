using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for execution result.
/// </summary>
public interface ExecutionResult
{
    /// <summary>
    /// Performs the evaluate operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task EvaluateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether faulted.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool IsFaulted([NotNullWhen(true)] out Exception? exception);
}
