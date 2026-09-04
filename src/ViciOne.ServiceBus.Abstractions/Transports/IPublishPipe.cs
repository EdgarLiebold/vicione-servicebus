using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

public interface IPublishPipe :
    IProbeSite
{
    Task Send<T>(PublishContext<T> context)
        where T : class;
}
