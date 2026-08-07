// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public interface IHandlerConnectorCache<T>
        where T : class
    {
        IHandlerConnector<T> Connector { get; }
    }
}
