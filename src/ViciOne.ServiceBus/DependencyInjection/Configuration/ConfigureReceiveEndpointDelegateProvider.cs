// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    using System;


    public class ConfigureReceiveEndpointDelegateProvider :
        IConfigureReceiveEndpoint
    {
        readonly ConfigureEndpointsProviderCallback _callback;
        readonly IRegistrationContext _context;

        public ConfigureReceiveEndpointDelegateProvider(IRegistrationContext context, ConfigureEndpointsProviderCallback callback)
        {
            _context = context;
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public void Configure(string name, IReceiveEndpointConfigurator configurator)
        {
            _callback(_context, name, configurator);
        }
    }
}
