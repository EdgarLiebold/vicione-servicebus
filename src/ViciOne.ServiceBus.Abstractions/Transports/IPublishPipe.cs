using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

public interface IPublishPipe :
    IProbeSite
{
    Task SendAsync<T>(PublishContext<T> context, CancellationToken cancellationToken = default)
        where T : class;
}
