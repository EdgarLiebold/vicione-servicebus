namespace ViciOne.ServiceBus.Configuration
{
    public interface IConsumePipeSpecificationObserverConnector
    {
        ConnectHandle ConnectConsumePipeSpecificationObserver(IConsumePipeSpecificationObserver observer);
    }
}
