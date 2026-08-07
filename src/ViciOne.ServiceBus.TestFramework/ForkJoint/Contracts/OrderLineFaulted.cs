// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.ForkJoint.Contracts
{
    using System;


    public interface OrderLineFaulted :
        FutureFaulted
    {
        Guid OrderId { get; }
        Guid OrderLineId { get; }
        string Description { get; }
    }
}
