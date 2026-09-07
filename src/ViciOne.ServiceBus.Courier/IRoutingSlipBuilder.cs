using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Builds a complete routing slip from an itinerary, variables, and subscriptions.</summary>
public interface IRoutingSlipBuilder :
    IItineraryBuilder
{
    /// <summary>Builds the routing slip using the current state of the builder.</summary>
    /// <returns>An immutable routing-slip contract representing the accumulated builder state.</returns>
    RoutingSlip Build();
}
