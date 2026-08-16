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
