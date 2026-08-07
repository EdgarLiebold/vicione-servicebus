// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.ComponentModel;
    using Configuration;


    public interface IRetryConfigurator :
        IExceptionConfigurator,
        IRetryObserverConnector
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        void SetRetryPolicy(RetryPolicyFactory factory);
    }
}
