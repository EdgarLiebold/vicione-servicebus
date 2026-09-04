using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;
/// <summary>
/// Creates and caches a model on the connection
/// </summary>
public interface IClientContextSupervisor :
    ITransportSupervisor<ClientContext>
{
}
