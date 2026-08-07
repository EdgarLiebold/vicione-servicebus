// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    using Transports;


    public interface IBusInstanceSpecification :
        ISpecification
    {
        void Configure(IBusInstance busInstance);
    }
}
