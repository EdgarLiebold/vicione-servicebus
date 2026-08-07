// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    using System.Collections.Generic;


    public interface IRegistrationCache<out T>
    {
        IEnumerable<T> Values { get; }
    }
}
