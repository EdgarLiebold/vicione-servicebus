// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.KafkaIntegration
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Transports;


    public interface KafkaSendTransportContext<TKey, TValue> :
        SendTransportContext,
        IProbeSite
        where TValue : class
    {
        IEnumerable<IAgent> GetAgentHandles();

        Task<KafkaSendContext<TKey, TValue>> CreateContext(TKey key, TValue value, IPipe<KafkaSendContext<TKey, TValue>> pipe,
            CancellationToken cancellationToken, IPipe<SendContext<TValue>> initializerPipe = null);

        Task Send(ProducerContext producerContext, KafkaSendContext<TKey, TValue> sendContext);
        Task Send(IPipe<ProducerContext> pipe, CancellationToken cancellationToken);
    }
}
