// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MongoDbIntegration.Courier.Consumers
{
    using System.Threading.Tasks;
    using Events;
    using ViciOne.ServiceBus.Courier.Contracts;


    public class RoutingSlipCompletedConsumer :
        IConsumer<RoutingSlipCompleted>
    {
        readonly IRoutingSlipEventPersister _persister;

        public RoutingSlipCompletedConsumer(IRoutingSlipEventPersister persister)
        {
            _persister = persister;
        }

        public Task Consume(ConsumeContext<RoutingSlipCompleted> context)
        {
            var @event = new RoutingSlipCompletedDocument(context.Message);

            return _persister.Persist(context.Message.TrackingNumber, @event);
        }
    }
}
