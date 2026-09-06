using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Defines the operations required by execution result.</summary>
public interface ExecutionResult
{
    /// <summary>Evaluates the configured expression.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task EvaluateAsync(CancellationToken cancellationToken = default);

    /// <summary>Determines whether faulted.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool IsFaulted([NotNullWhen(true)] out Exception? exception);
}
