using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.DependencyInjection;

public interface IRegistrationRiderFactory<in TRider>
    where TRider : IRider
{
    IBusInstanceSpecification CreateRider(IRiderRegistrationContext context);
}
