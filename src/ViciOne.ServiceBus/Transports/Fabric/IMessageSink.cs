using System.Threading.Tasks;

#nullable enable
namespace ViciOne.ServiceBus.Transports.Fabric;

public interface IMessageSink<T> :
    IProbeSite
    where T : class
{
    Task Deliver(DeliveryContext<T> context);
}
