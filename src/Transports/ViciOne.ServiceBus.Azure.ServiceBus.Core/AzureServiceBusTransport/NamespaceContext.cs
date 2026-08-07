// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureServiceBusTransport
{
    /// <summary>
    /// A service bus namespace which has the appropriate messaging factories available
    /// </summary>
    public interface NamespaceContext :
        PipeContext
    {
        ConnectionContext ConnectionContext { get; }
    }
}
