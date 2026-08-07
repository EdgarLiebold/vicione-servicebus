// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AmazonSqsTransport;

using Transports;


/// <summary>
/// Creates and caches a model on the connection
/// </summary>
public interface IClientContextSupervisor :
    ITransportSupervisor<ClientContext>
{
}
