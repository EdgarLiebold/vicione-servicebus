using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

class RegistrationFilter :
    IRegistrationFilter
{
    readonly CompositeFilter<Type> _filter;

    public RegistrationFilter(CompositeFilter<Type> filter)
    {
        _filter = filter;
    }

    public bool Matches(IRegistration registration)
    {
        return _filter.Matches(registration.Type);
    }
}
