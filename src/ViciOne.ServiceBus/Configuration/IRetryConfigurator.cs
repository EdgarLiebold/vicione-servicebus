using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures retry.</summary>
public interface IRetryConfigurator :
    IExceptionConfigurator,
    IRetryObserverConnector
{
    /// <summary>Sets retry policy.</summary>
    /// <param name="factory">The factory invoked by the operation.</param>
    void SetRetryPolicy(RetryPolicyFactory factory);
}
