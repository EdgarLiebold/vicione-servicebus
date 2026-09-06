using System;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>
/// A retry policy determines how exceptions are handled, and whether or not the
/// remaining filters should be retried.
/// </summary>
public interface IRetryPolicy :
    IProbeSite
{
    /// <summary>Creates a retry policy context for the retry, which initiates the exception tracking.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The created policy context.</returns>
    RetryPolicyContext<T> CreatePolicyContext<T>(T context)
        where T : class, PipeContext;

    /// <summary>If the retry policy handles the exception, should return true.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool IsHandled(Exception exception);
}
