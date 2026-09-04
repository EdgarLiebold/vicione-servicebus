using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

public interface ITransportSendEndpoint :
    ISendEndpoint,
    Advanced.IAdvancedSendEndpoint
{
    Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class;
}
