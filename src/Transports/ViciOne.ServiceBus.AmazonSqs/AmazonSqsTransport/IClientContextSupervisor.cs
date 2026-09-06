using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;
/// <summary>Supervises shared and operation-scoped Amazon client contexts.</summary>
public interface IClientContextSupervisor :
    ITransportSupervisor<ClientContext>
{
}
