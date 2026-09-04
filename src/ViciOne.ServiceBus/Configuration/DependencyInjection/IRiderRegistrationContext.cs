using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus;

public interface IRiderRegistrationContext :
    IRegistrationContext
{
    IEnumerable<T> GetRegistrations<T>()
        where T : class, IRegistration;
}
