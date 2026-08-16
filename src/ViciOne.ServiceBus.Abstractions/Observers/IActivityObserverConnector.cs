namespace ViciOne.ServiceBus
{
    public interface IActivityObserverConnector
    {
        ConnectHandle ConnectActivityObserver(IActivityObserver observer);
    }
}
