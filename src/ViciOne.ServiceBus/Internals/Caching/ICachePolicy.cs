// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Internals.Caching
{
    using System;


    public interface ICachePolicy<TValue, TCacheValue>
        where TValue : class
        where TCacheValue : ICacheValue<TValue>
    {
        TCacheValue CreateValue(Action remove);

        bool IsValid(TCacheValue value);

        int CheckValue(TCacheValue value);
    }
}
