using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates registration bus instances.</summary>
public interface IRegistrationBusFactory
{
    /// <summary>Creates bus.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="specifications">The specifications.</param>
    /// <param name="busName">The bus name.</param>
    /// <returns>The created bus.</returns>
    IBusInstance CreateBus(IBusRegistrationContext context, IEnumerable<IBusInstanceSpecification> specifications, string busName);
}
