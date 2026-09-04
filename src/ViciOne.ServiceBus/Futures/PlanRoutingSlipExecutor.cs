using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Futures;

public class PlanRoutingSlipExecutor<TInput> :
    IRoutingSlipExecutor<TInput>
    where TInput : class
{
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

    public bool TrackRoutingSlip { get; set; }
}
