// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.Collections.Generic;
    using Configuration;


    public interface IRiderRegistrationContext :
        IRegistrationContext
    {
        IEnumerable<T> GetRegistrations<T>()
            where T : class, IRegistration;
    }
}
