using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configure a message handler, including specifying filters that are executed around
/// the handler itself
/// </summary>
public interface IRoutingSlipConfigurator :
    IActivityMessageConfigurator<RoutingSlip>
{
}
