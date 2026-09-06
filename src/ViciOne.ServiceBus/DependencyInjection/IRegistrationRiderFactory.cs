using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Creates registration rider instances.</summary>
/// <typeparam name="TRider">The rider type.</typeparam>
public interface IRegistrationRiderFactory<in TRider>
    where TRider : IRider
{
    /// <summary>Creates rider.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The created rider.</returns>
    IBusInstanceSpecification CreateRider(IRiderRegistrationContext context);
}
