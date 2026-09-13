using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures middleware for the routing-slip message consumed by an activity host.
/// </summary>
public interface IRoutingSlipConfigurator :
    IActivityMessageConfigurator<IRoutingSlip>
{
}
