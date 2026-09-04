using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Defines the contract for registration rider factory.
/// </summary>
/// <typeparam name="TRider">The t rider type.</typeparam>
public interface IRegistrationRiderFactory<in TRider>
    where TRider : IRider
{
    /// <summary>
    /// Creates rider.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    IBusInstanceSpecification CreateRider(IRiderRegistrationContext context);
}
