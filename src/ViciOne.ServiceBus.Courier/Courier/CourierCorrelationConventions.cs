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

            GlobalTopology.UseCapabilityCorrelationId<IRoutingSlip>(message => message.TrackingNumber);
            GlobalTopology.UseCapabilityCorrelationId<IRoutingSlipCompleted>(message => message.TrackingNumber);
            GlobalTopology.UseCapabilityCorrelationId<IRoutingSlipFaulted>(message => message.TrackingNumber);
            GlobalTopology.UseCapabilityCorrelationId<IRoutingSlipActivityCompleted>(message => message.ExecutionId);
            GlobalTopology.UseCapabilityCorrelationId<IRoutingSlipActivityFaulted>(message => message.ExecutionId);
            GlobalTopology.UseCapabilityCorrelationId<IRoutingSlipActivityCompensated>(message => message.ExecutionId);
            GlobalTopology.UseCapabilityCorrelationId<IRoutingSlipActivityCompensationFailed>(message => message.ExecutionId);
            GlobalTopology.UseCapabilityCorrelationId<IRoutingSlipCompensationFailed>(message => message.TrackingNumber);
            GlobalTopology.UseCapabilityCorrelationId<IRoutingSlipTerminated>(message => message.TrackingNumber);
            GlobalTopology.UseCapabilityCorrelationId<IRoutingSlipRevised>(message => message.TrackingNumber);

            _registered = true;
        }
    }
}
