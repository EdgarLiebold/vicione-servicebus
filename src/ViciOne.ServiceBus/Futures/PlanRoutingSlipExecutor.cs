using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Executes plan routing slip operations.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
public class PlanRoutingSlipExecutor<TInput> :
    IRoutingSlipExecutor<TInput>
    where TInput : class
{
    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>Gets or sets the track routing slip.</summary>
    public bool TrackRoutingSlip { get; set; }
}
