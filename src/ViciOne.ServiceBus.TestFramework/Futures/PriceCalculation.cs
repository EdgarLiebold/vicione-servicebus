// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.Futures
{
    using System;


    public interface PriceCalculation
    {
        Guid OrderLineId { get; }

        decimal Amount { get; }
    }
}
