// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.DependencyInjection
{
    using Configuration;
    using Transports;


    public interface IRegistrationRiderFactory<in TRider>
        where TRider : IRider
    {
        IBusInstanceSpecification CreateRider(IRiderRegistrationContext context);
    }
}
