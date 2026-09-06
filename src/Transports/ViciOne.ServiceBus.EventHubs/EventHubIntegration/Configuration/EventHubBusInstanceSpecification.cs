using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>Validates the Event Hubs rider configuration and attaches the built rider to a bus instance.</summary>
public class EventHubBusInstanceSpecification :
    IBusInstanceSpecification
{
    readonly IRiderRegistrationContext _context;
    readonly IEventHubHostConfiguration _hostConfiguration;

    /// <summary>Creates a bus-instance specification for the configured Event Hubs rider.</summary>
    /// <param name="context">The rider registration context used to build the rider.</param>
    /// <param name="hostConfiguration">The Event Hubs host configuration.</param>
    public EventHubBusInstanceSpecification(IRiderRegistrationContext context, IEventHubHostConfiguration hostConfiguration)
    {
        _context = context;
        _hostConfiguration = hostConfiguration;
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _hostConfiguration.Validate();
    }

    /// <summary>Builds and connects the Event Hubs rider.</summary>
    /// <param name="busInstance">The bus instance that will own the rider.</param>
    public void Configure(IBusInstance busInstance)
    {
        var rider = _hostConfiguration.Build(_context, busInstance);
        busInstance.Connect<IEventHubRider>(rider);
    }
}
