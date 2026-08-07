// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MongoDbIntegration.Courier.Consumers
{
    using System.Threading.Tasks;
    using Events;
    using ViciOne.ServiceBus.Courier.Contracts;


    public class RoutingSlipActivityCompensationFailedConsumer :
        IConsumer<RoutingSlipActivityCompensationFailed>
    {
        readonly IRoutingSlipEventPersister _persister;

        public RoutingSlipActivityCompensationFailedConsumer(IRoutingSlipEventPersister persister)
        {
            _persister = persister;
        }

        public Task Consume(ConsumeContext<RoutingSlipActivityCompensationFailed> context)
        {
            var @event = new RoutingSlipActivityCompensationFailedDocument(context.Message);

            return _persister.Persist(context.Message.TrackingNumber, @event);
        }
    }
}
