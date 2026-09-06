using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds activities and variables to a future's routing-slip itinerary.</summary>
/// <typeparam name="TInput">The input message contract.</typeparam>
/// <param name="context">The future behavior context containing the input message.</param>
/// <param name="builder">The itinerary builder to populate.</param>
/// <returns>A task that completes when the itinerary has been configured.</returns>
public delegate Task BuildItineraryCallback<in TInput>(BehaviorContext<FutureState, TInput> context, IItineraryBuilder builder)
    where TInput : class;
