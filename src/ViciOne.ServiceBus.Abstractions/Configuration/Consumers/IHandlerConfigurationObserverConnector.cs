namespace ViciOne.ServiceBus
{
    using System.ComponentModel;


    public interface IHandlerConfigurationObserverConnector
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        ConnectHandle ConnectHandlerConfigurationObserver(IHandlerConfigurationObserver observer);
    }
}
