// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.ForkJoint.Contracts
{
    using System;


    public interface SubmitOrder
    {
        Guid OrderId { get; }

        Burger[] Burgers { get; }
        Fry[] Fries { get; }
        Shake[] Shakes { get; }
        FryShake[] FryShakes { get; }
    }
}
