// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.ForkJoint.Contracts
{
    using System;
    using System.Collections.Generic;


    public interface OrderFaulted :
        FutureFaulted
    {
        Guid OrderId { get; }

        IDictionary<Guid, OrderLineCompleted> LinesCompleted { get; }

        IDictionary<Guid, Fault<OrderLine>> LinesFaulted { get; }
    }
}
