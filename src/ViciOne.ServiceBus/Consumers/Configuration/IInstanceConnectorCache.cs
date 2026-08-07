// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public interface IInstanceConnectorCache<T>
        where T : class
    {
        IInstanceConnector Connector { get; }
    }
}
