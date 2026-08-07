// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using Middleware;


    public interface IPipeConnectorSpecification :
        ISpecification
    {
        void Connect(IPipeConnector connector);
    }
}
