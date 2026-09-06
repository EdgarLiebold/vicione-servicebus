using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Builds and executes a routing slip with a configured callback.</summary>
/// <typeparam name="TInput">The future event contract supplied to the itinerary builder.</typeparam>
internal sealed class BuildRoutingSlipExecutor<TInput> :
    IRoutingSlipExecutor<TInput>
    where TInput : class
{
    readonly BuildItineraryCallback<TInput> _buildItinerary;

    /// <summary>Creates an executor that builds itineraries with the supplied callback.</summary>
    /// <param name="buildItinerary">The callback that adds activities and variables to each routing slip.</param>
    public BuildRoutingSlipExecutor(BuildItineraryCallback<TInput> buildItinerary)
    {
        ArgumentNullException.ThrowIfNull(buildItinerary);
        _buildItinerary = buildItinerary;
    }

    /// <summary>Builds and executes a routing slip correlated to the current future.</summary>
    /// <param name="context">The future event context used to build the itinerary.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync(BehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        var trackingNumber = NewId.NextGuid();

        var builder = new RoutingSlipBuilder(trackingNumber);

        builder.AddVariable(MessageHeaders.FutureId, context.CorrelationId);

        builder.AddSubscription(context.ReceiveContext.InputAddress, RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

        await _buildItinerary(context, builder).ConfigureAwait(false);

        var routingSlip = builder.Build();

        await context.ExecuteAsync(routingSlip, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (TrackRoutingSlip)
            context.Saga.Pending.Add(trackingNumber);
    }

    /// <summary>Gets or sets whether the routing slip remains pending until its terminal event is consumed.</summary>
    public bool TrackRoutingSlip { get; set; }
}
