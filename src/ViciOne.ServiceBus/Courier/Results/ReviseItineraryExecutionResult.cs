using System;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Results;

class ReviseItineraryExecutionResult<TArguments> :
    CompletedExecutionResult<TArguments>
    where TArguments : class
{
    readonly Action<IItineraryBuilder> _itineraryBuilder;

    public ReviseItineraryExecutionResult(ExecuteContext<TArguments> context, IRoutingSlipEventPublisher publisher, Activity activity,
        RoutingSlip routingSlip, Uri compensationAddress, Action<IItineraryBuilder> itineraryBuilder)
        : base(context, publisher, activity, routingSlip, compensationAddress)
    {
        _itineraryBuilder = itineraryBuilder;
    }

    protected override void Build(RoutingSlipBuilder builder)
    {
        base.Build(builder);

        _itineraryBuilder(builder);
    }

    protected override RoutingSlipBuilder CreateRoutingSlipBuilder(RoutingSlip routingSlip)
    {
        return new RoutingSlipBuilder(routingSlip, [], routingSlip.Itinerary.Skip(1));
    }

    protected override async Task PublishActivityEventsAsync(RoutingSlip routingSlip, RoutingSlipBuilder builder)
    {
        await base.PublishActivityEventsAsync(routingSlip, builder).ConfigureAwait(false);

        await Publisher.PublishRoutingSlipRevisedAsync(Context.ActivityName, Context.ExecutionId, Context.Timestamp, Context.Elapsed, routingSlip.Variables,
            routingSlip.Itinerary, builder.SourceItinerary).ConfigureAwait(false);
    }
}
