namespace ViciOne.ServiceBus
{
    using System.ComponentModel;


    public interface IConsumerConfigurationObserverConnector
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer);
    }
}
