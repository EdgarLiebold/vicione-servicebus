using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Futures;

/// <summary>
/// Implement to build a routing slip. This can be resolved by a durable future to build
/// a routing slip at runtime in response to an input command.
/// </summary>
/// <typeparam name="TInput">The input message type</typeparam>
public interface IItineraryPlanner<in TInput>
    where TInput : class
{
    /// <summary>
    /// Performs the plan itinerary operation.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="builder">The builder value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task PlanItineraryAsync(BehaviorContext<FutureState, TInput> value, IItineraryBuilder builder, CancellationToken cancellationToken = default);
}
