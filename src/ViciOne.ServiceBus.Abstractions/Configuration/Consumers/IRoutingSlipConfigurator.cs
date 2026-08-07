// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using Courier.Contracts;


    /// <summary>
    /// Configure a message handler, including specifying filters that are executed around
    /// the handler itself
    /// </summary>
    public interface IRoutingSlipConfigurator :
        IConsumeConfigurator,
        IPipeConfigurator<ConsumeContext<RoutingSlip>>
    {
    }
}
