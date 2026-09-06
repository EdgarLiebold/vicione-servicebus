using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Builds routing slip components.</summary>
public interface IRoutingSlipBuilder :
    IItineraryBuilder
{
    /// <summary>Builds the routing slip using the current state of the builder.</summary>
    /// <returns>The RoutingSlip.</returns>
    RoutingSlip Build();
}
