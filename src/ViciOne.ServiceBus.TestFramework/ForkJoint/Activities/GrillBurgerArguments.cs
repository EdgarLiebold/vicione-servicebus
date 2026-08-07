// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.ForkJoint.Activities
{
    using System;


    public interface GrillBurgerArguments
    {
        Guid OrderId { get; }
        Guid BurgerId { get; }

        decimal Weight { get; }
        bool Cheese { get; }
    }
}
