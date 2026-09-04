using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubIntegration.Configuration;

public class EventHubBusInstanceSpecification :
    IBusInstanceSpecification
{
    readonly IRiderRegistrationContext _context;
    readonly IEventHubHostConfiguration _hostConfiguration;

    public EventHubBusInstanceSpecification(IRiderRegistrationContext context, IEventHubHostConfiguration hostConfiguration)
    {
        _context = context;
        _hostConfiguration = hostConfiguration;
    }

    public IEnumerable<ValidationResult> Validate()
    {
        return _hostConfiguration.Validate();
    }

    public void Configure(IBusInstance busInstance)
    {
        var rider = _hostConfiguration.Build(_context, busInstance);
        busInstance.Connect<IEventHubRider>(rider);
    }
}
