// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MongoDbIntegration.Courier.Consumers
{
    using System.Threading.Tasks;
    using Events;
    using ViciOne.ServiceBus.Courier.Contracts;


    public class RoutingSlipFaultedConsumer :
        IConsumer<RoutingSlipFaulted>
    {
        readonly IRoutingSlipEventPersister _persister;

        public RoutingSlipFaultedConsumer(IRoutingSlipEventPersister persister)
        {
            _persister = persister;
        }

        public Task Consume(ConsumeContext<RoutingSlipFaulted> context)
        {
            var @event = new RoutingSlipFaultedDocument(context.Message);

            return _persister.Persist(context.Message.TrackingNumber, @event);
        }
    }
}
