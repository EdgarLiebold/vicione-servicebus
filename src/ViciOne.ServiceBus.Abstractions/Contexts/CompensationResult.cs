using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Defines the operations required by compensation result.</summary>
public interface CompensationResult
{
    /// <summary>Evaluates the configured expression.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task EvaluateAsync(CancellationToken cancellationToken = default);

    /// <summary>Determines whether failed.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool IsFailed([NotNullWhen(true)] out Exception? exception);
}
