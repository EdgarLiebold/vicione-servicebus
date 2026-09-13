using System;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures the routing slip that a future executes for an input message.</summary>
/// <typeparam name="TResult">The successful future result contract.</typeparam>
/// <typeparam name="TFault">The future fault contract.</typeparam>
/// <typeparam name="TInput">The input message contract.</typeparam>
public interface IFutureRoutingSlipConfigurator<TResult, TFault, out TInput>
    where TResult : class
    where TFault : class
    where TInput : class
{
    /// <summary>
    /// If specified, the routing slip is added to the pending results, using the routing slip tracking
    /// number. When the routing slip completes or faults, the pending result is completed or faulted.
    /// </summary>
    void TrackPendingRoutingSlip();

    /// <summary>
    /// Builds the routing slip itinerary when the command is received. The routing slip builder
    /// is passed, along with the <see cref="IBehaviorContext{FutureState,TInput}" />. The tracking numbers,
    /// subscriptions, and FutureId variables are already initialized.
    /// </summary>
    /// <param name="buildItinerary">The callback that populates the routing-slip itinerary.</param>
    void BuildItinerary(BuildItineraryCallback<TInput> buildItinerary);

    /// <summary>
    /// Builds the routing slip itinerary when the command is received using a container-registered
    /// <see cref="IItineraryPlanner{TInput}" />.
    /// </summary>
    void BuildUsingItineraryPlanner();

    /// <summary>Configures how routing-slip completion produces the successful future result.</summary>
    /// <param name="configure">The callback that configures the future result message.</param>
    void OnRoutingSlipCompleted(Action<IFutureResultConfigurator<TResult, IRoutingSlipCompleted>> configure);

    /// <summary>Configures how a routing-slip fault produces the terminal future fault.</summary>
    /// <param name="configure">The callback that configures the future fault message.</param>
    void OnRoutingSlipFaulted(Action<IFutureFaultConfigurator<TFault, IRoutingSlipFaulted>> configure);

    /// <summary>Adds state-machine activities executed when the routing slip completes.</summary>
    /// <param name="configure">The callback that adds activities to the completion event.</param>
    void WhenRoutingSlipCompleted(
        Func<IEventActivityBinder<FutureState, IRoutingSlipCompleted>, IEventActivityBinder<FutureState, IRoutingSlipCompleted>> configure);

    /// <summary>Adds state-machine activities executed when the routing slip faults.</summary>
    /// <param name="configure">The callback that adds activities to the fault event.</param>
    void WhenRoutingSlipFaulted(Func<IEventActivityBinder<FutureState, IRoutingSlipFaulted>, IEventActivityBinder<FutureState, IRoutingSlipFaulted>> configure);
}
