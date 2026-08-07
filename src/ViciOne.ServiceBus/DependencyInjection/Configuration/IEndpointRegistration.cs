// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    using System;


    public interface IEndpointRegistration :
        IRegistration
    {
        IEndpointDefinition GetDefinition(IServiceProvider provider);
    }
}
