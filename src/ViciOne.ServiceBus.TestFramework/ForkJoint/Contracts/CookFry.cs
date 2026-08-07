// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.ForkJoint.Contracts
{
    using System;


    public interface CookFry
    {
        Guid OrderId { get; }
        Guid OrderLineId { get; }

        Size Size { get; }
    }


    class CookFryRequest :
        CookFry
    {
        public CookFryRequest(Guid orderId, Guid orderLineId, Size size)
        {
            OrderId = orderId;
            OrderLineId = orderLineId;
            Size = size;
        }

        public Guid OrderId { get; }
        public Guid OrderLineId { get; }
        public Size Size { get; }
    }
}
