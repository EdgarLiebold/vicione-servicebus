using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Called by the future to build the routing slip.</summary>
/// <typeparam name="TInput">The input message type.</typeparam>
/// <param name="context">The input consume context.</param>
/// <param name="builder">The routing slip itinerary builder.</param>
/// <returns>The value produced by the operation.</returns>
public delegate Task BuildItineraryCallback<in TInput>(BehaviorContext<FutureState, TInput> context, IItineraryBuilder builder)
    where TInput : class;
