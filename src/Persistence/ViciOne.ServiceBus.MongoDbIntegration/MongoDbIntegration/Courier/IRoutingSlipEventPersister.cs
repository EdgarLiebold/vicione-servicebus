// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MongoDbIntegration.Courier
{
    using System;
    using System.Threading.Tasks;
    using Events;


    public interface IRoutingSlipEventPersister
    {
        Task Persist<T>(Guid trackingNumber, T @event)
            where T : RoutingSlipEventDocument;
    }
}
