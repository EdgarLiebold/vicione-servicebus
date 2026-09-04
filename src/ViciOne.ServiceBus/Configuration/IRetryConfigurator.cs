using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for retry configurator.
/// </summary>
public interface IRetryConfigurator :
    IExceptionConfigurator,
    IRetryObserverConnector
{
    /// <summary>
    /// Sets retry policy.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    void SetRetryPolicy(RetryPolicyFactory factory);
}
