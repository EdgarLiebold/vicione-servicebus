using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Resolves an itinerary planner and executes its routing slip for a future input.</summary>
/// <typeparam name="TInput">The future event contract supplied to the itinerary planner.</typeparam>
internal sealed class PlanRoutingSlipExecutor<TInput> :
    IRoutingSlipExecutor<TInput>
    where TInput : class
{
    /// <summary>Resolves the planner, builds its routing slip, and executes it for the current future.</summary>
    /// <param name="context">The future event context used to resolve and invoke the planner.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync(BehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        var itineraryPlanner = context.GetServiceOrCreateInstance<IItineraryPlanner<TInput>>();

        var trackingNumber = NewId.NextGuid();

        var builder = new RoutingSlipBuilder(trackingNumber);

        builder.SetVariable(MessageHeaders.FutureId, context.CorrelationId);

        builder.AddSubscription(context.ReceiveContext.InputAddress, RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

        await itineraryPlanner.PlanItineraryAsync(context, builder, cancellationToken: cancellationToken).ConfigureAwait(false);

        var routingSlip = builder.Build();

        await context.ExecuteAsync(routingSlip, cancellationToken).ConfigureAwait(false);

        if (TrackRoutingSlip)
            context.Saga.Pending.Add(trackingNumber);
    }

    /// <summary>Gets or sets whether the routing slip remains pending until its terminal event is consumed.</summary>
    public bool TrackRoutingSlip { get; set; }
}
