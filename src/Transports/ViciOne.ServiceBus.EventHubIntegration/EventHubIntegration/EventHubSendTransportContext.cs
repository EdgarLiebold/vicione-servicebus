using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubIntegration;

public interface EventHubSendTransportContext :
    SendTransportContext,
    IProbeSite
{
    IEnumerable<IAgent> GetAgentHandles();

    Task<EventHubSendContext<T>> CreateContextAsync<T>(T value, IPipe<EventHubSendContext<T>> pipe,
        IPipe<SendContext<T>>? initializerPipe = null, CancellationToken cancellationToken = default)
        where T : class;

    Task SendAsync<T>(ProducerContext producerContext, EventHubSendContext<T> sendContext, CancellationToken cancellationToken = default)
        where T : class;

    Task SendAsync<T>(ProducerContext producerContext, EventHubSendContext<T>[] sendContexts, CancellationToken cancellationToken = default)
        where T : class;

    Task SendAsync(IPipe<ProducerContext> pipe, CancellationToken cancellationToken);
}
