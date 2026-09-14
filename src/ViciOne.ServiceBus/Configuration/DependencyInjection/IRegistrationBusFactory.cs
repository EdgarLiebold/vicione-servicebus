using System.Collections.Generic;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates the transport runtime for a dependency-injected bus registration.</summary>
public interface IRegistrationBusFactory
{
    /// <summary>Creates one bus instance from its registration context and runtime specifications.</summary>
    /// <param name="context">The bus registration context.</param>
    /// <param name="specifications">The bus-instance specifications to validate and apply.</param>
    /// <param name="busName">The configured bus name.</param>
    /// <returns>The transport-backed bus instance.</returns>
    IBusInstance CreateBus(IBusRegistrationContext context, IEnumerable<IBusInstanceSpecification> specifications, string busName);
}
