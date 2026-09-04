using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for registration bus factory.
/// </summary>
public interface IRegistrationBusFactory
{
    /// <summary>
    /// Creates bus.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="specifications">The specifications value.</param>
    /// <param name="busName">The bus name value.</param>
    /// <returns>The result of the operation.</returns>
    IBusInstance CreateBus(IBusRegistrationContext context, IEnumerable<IBusInstanceSpecification> specifications, string busName);
}
