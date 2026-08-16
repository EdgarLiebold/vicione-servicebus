namespace ViciOne.ServiceBus
{
    using System.ComponentModel;


    public interface ISagaConfigurationObserverConnector
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        ConnectHandle ConnectSagaConfigurationObserver(ISagaConfigurationObserver observer);
    }
}
