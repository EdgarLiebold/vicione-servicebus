using System;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Creates per-operation retry state and classifies failures for that state.</summary>
public interface IRetryPolicy :
    IProbeSite
{
    /// <summary>Creates isolated retry state for one pipeline operation.</summary>
    /// <typeparam name="T">The pipeline context type.</typeparam>
    /// <param name="context">The pipeline context governed by the policy.</param>
    /// <returns>The retry state for the operation.</returns>
    RetryPolicyContext<T> CreatePolicyContext<T>(T context)
        where T : class, PipeContext;

    /// <summary>Determines whether an exception is eligible for this policy.</summary>
    /// <param name="exception">The exception to classify.</param>
    /// <returns><see langword="true" /> when the policy handles the exception; otherwise, <see langword="false" />.</returns>
    bool IsHandled(Exception exception);
}
