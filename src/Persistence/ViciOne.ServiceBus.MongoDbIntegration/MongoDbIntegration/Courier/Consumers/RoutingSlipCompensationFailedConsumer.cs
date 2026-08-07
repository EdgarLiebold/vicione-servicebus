// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MongoDbIntegration.Courier.Consumers
{
    using System.Threading.Tasks;
    using Events;
    using ViciOne.ServiceBus.Courier.Contracts;


    public class RoutingSlipCompensationFailedConsumer :
        IConsumer<RoutingSlipCompensationFailed>
    {
        readonly IRoutingSlipEventPersister _persister;

        public RoutingSlipCompensationFailedConsumer(IRoutingSlipEventPersister persister)
        {
            _persister = persister;
        }

        public Task Consume(ConsumeContext<RoutingSlipCompensationFailed> context)
        {
            var @event = new RoutingSlipCompensationFailedDocument(context.Message);

            return _persister.Persist(context.Message.TrackingNumber, @event);
        }
    }
}
