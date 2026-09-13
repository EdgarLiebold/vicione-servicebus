using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Futures;

/// <summary>
/// Implement to build a routing slip. This can be resolved by a durable future to build
/// a routing slip at runtime in response to an input command.
/// </summary>
/// <typeparam name="TInput">The input message type.</typeparam>
public interface IItineraryPlanner<in TInput>
    where TInput : class
{
    /// <summary>Adds activities and variables to the routing-slip itinerary.</summary>
    /// <param name="context">The future event that supplies itinerary data and services.</param>
    /// <param name="builder">The builder that receives the configuration.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PlanItineraryAsync(IBehaviorContext<FutureState, TInput> context, IItineraryBuilder builder, CancellationToken cancellationToken = default);
}
