namespace ViciOne.ServiceBus.Configuration
{
    public interface IInstanceConnectorCache<T>
        where T : class
    {
        IInstanceConnector Connector { get; }
    }
}
