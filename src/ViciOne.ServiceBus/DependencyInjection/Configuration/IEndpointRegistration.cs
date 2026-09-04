using System;

namespace ViciOne.ServiceBus.Configuration;

public interface IEndpointRegistration :
    IRegistration
{
    IEndpointDefinition GetDefinition(IServiceProvider provider);
}
