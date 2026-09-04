using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Futures;

/// <summary>
/// Provides a build routing slip executor implementation.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
public class BuildRoutingSlipExecutor<TInput> :
    IRoutingSlipExecutor<TInput>
    where TInput : class
{
    readonly BuildItineraryCallback<TInput> _buildItinerary;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="buildItinerary">The build itinerary value.</param>
    public BuildRoutingSlipExecutor(BuildItineraryCallback<TInput> buildItinerary)
    {
        _buildItinerary = buildItinerary;
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Gets or sets the track routing slip value.
    /// </summary>
    public bool TrackRoutingSlip { get; set; }
}
