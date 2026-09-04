using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

public interface ISendContextPipe
{
    Task SendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default)
        where T : class;
}
