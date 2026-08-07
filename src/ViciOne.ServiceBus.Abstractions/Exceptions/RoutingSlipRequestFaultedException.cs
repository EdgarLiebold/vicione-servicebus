// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using Courier.Contracts;


    public class RoutingSlipRequestFaultedException :
        RoutingSlipException
    {
        public RoutingSlipRequestFaultedException(RoutingSlipFaulted faulted)
            : base("The routing slip request faulted")
        {
            Faulted = faulted;
        }

        public RoutingSlipFaulted Faulted { get; }
    }
}
