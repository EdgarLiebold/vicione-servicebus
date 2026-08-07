// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.ForkJoint.Activities
{
    using System;
    using Contracts;


    public interface DressBurgerArguments
    {
        Guid OrderId { get; }
        Guid BurgerId { get; }

        BurgerPatty Patty { get; }

        bool Lettuce { get; }
        bool Pickle { get; }
        bool Onion { get; }
        bool Ketchup { get; }
        bool Mustard { get; }
        bool BarbecueSauce { get; }
        bool OnionRing { get; }

        Guid? OnionRingId { get; }
    }
}
