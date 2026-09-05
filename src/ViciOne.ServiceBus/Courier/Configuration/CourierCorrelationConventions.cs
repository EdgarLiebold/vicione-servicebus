using ViciOne.ServiceBus.Advanced.Topology;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier;

static class CourierCorrelationConventions
{
    static readonly object Sync = new();
    static bool _registered;

    internal static void Register()
    {
        lock (Sync)
        {
            if (_registered)
                return;

            GlobalTopology.UseCapabilityCorrelationId<RoutingSlip>(message => message.TrackingNumber);
            GlobalTopology.UseCapabilityCorrelationId<RoutingSlipCompleted>(message => message.TrackingNumber);
            GlobalTopology.UseCapabilityCorrelationId<RoutingSlipFaulted>(message => message.TrackingNumber);
            GlobalTopology.UseCapabilityCorrelationId<RoutingSlipActivityCompleted>(message => message.ExecutionId);
            GlobalTopology.UseCapabilityCorrelationId<RoutingSlipActivityFaulted>(message => message.ExecutionId);
            GlobalTopology.UseCapabilityCorrelationId<RoutingSlipActivityCompensated>(message => message.ExecutionId);
            GlobalTopology.UseCapabilityCorrelationId<RoutingSlipActivityCompensationFailed>(message => message.ExecutionId);
            GlobalTopology.UseCapabilityCorrelationId<RoutingSlipCompensationFailed>(message => message.TrackingNumber);
            GlobalTopology.UseCapabilityCorrelationId<RoutingSlipTerminated>(message => message.TrackingNumber);
            GlobalTopology.UseCapabilityCorrelationId<RoutingSlipRevised>(message => message.TrackingNumber);

            _registered = true;
        }
    }
}
