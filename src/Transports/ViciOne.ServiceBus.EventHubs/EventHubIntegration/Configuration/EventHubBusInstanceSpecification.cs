using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>
/// Provides an event hub bus instance specification implementation.
/// </summary>
public class EventHubBusInstanceSpecification :
    IBusInstanceSpecification
{
    readonly IRiderRegistrationContext _context;
    readonly IEventHubHostConfiguration _hostConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="hostConfiguration">The host configuration value.</param>
    public EventHubBusInstanceSpecification(IRiderRegistrationContext context, IEventHubHostConfiguration hostConfiguration)
    {
        _context = context;
        _hostConfiguration = hostConfiguration;
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _hostConfiguration.Validate();
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="busInstance">The bus instance value.</param>
    public void Configure(IBusInstance busInstance)
    {
        var rider = _hostConfiguration.Build(_context, busInstance);
        busInstance.Connect<IEventHubRider>(rider);
    }
}
