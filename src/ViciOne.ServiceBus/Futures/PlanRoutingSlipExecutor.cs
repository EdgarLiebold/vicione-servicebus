using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Futures;

/// <summary>
/// Provides a plan routing slip executor implementation.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
public class PlanRoutingSlipExecutor<TInput> :
    IRoutingSlipExecutor<TInput>
    where TInput : class
{
    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ExecuteAsync(BehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default)
    {
        var itineraryPlanner = context.GetServiceOrCreateInstance<IItineraryPlanner<TInput>>();

        var trackingNumber = NewId.NextGuid();

        var builder = new RoutingSlipBuilder(trackingNumber);

        builder.AddVariable(MessageHeaders.FutureId, context.CorrelationId);

        builder.AddSubscription(context.ReceiveContext.InputAddress, RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

        await itineraryPlanner.PlanItineraryAsync(context, builder, cancellationToken: cancellationToken).ConfigureAwait(false);

        var routingSlip = builder.Build();

        await context.ExecuteAsync(routingSlip, context.CancellationToken).ConfigureAwait(false);

        if (TrackRoutingSlip)
            context.Saga.Pending.Add(trackingNumber);
    }

    /// <summary>
    /// Gets or sets the track routing slip value.
    /// </summary>
    public bool TrackRoutingSlip { get; set; }
}
