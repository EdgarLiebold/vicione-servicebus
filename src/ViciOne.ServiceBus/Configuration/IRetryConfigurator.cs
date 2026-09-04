using System.ComponentModel;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus;

public interface IRetryConfigurator :
    IExceptionConfigurator,
    IRetryObserverConnector
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    void SetRetryPolicy(RetryPolicyFactory factory);
}
