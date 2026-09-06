using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Executes build routing slip operations.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
public class BuildRoutingSlipExecutor<TInput> :
    IRoutingSlipExecutor<TInput>
    where TInput : class
{
    readonly BuildItineraryCallback<TInput> _buildItinerary;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="buildItinerary">The build itinerary.</param>
    public BuildRoutingSlipExecutor(BuildItineraryCallback<TInput> buildItinerary)
    {
        _buildItinerary = buildItinerary;
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync(BehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default)
    {
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

    /// <summary>Gets or sets the track routing slip.</summary>
    public bool TrackRoutingSlip { get; set; }
}
