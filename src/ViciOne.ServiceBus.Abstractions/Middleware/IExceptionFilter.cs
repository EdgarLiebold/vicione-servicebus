using System;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Determines whether an exception is selected for policy handling.</summary>
public interface IExceptionFilter :
    IProbeSite
{
    /// <summary>Determines whether the policy applies to an exception.</summary>
    /// <param name="exception">The exception to evaluate.</param>
    /// <returns><see langword="true" /> when the policy applies; otherwise, <see langword="false" />.</returns>
    bool Match(Exception exception);
}
