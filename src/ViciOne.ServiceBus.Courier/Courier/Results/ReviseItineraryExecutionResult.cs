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

    public ReviseItineraryExecutionResult(ExecuteContext<TArguments> context, IRoutingSlipEventPublisher publisher, IActivity activity,
        IRoutingSlip routingSlip, Uri? compensationAddress, Action<IItineraryBuilder> itineraryBuilder)
        : base(context, publisher, activity, routingSlip, compensationAddress)
    {
        ArgumentNullException.ThrowIfNull(itineraryBuilder);

        _itineraryBuilder = itineraryBuilder;
    }

    protected override void Build(RoutingSlipBuilder builder)
    {
        base.Build(builder);

        _itineraryBuilder(builder);
    }

    protected override RoutingSlipBuilder CreateRoutingSlipBuilder(IRoutingSlip routingSlip)
    {
        return new RoutingSlipBuilder(routingSlip, [], routingSlip.Itinerary.Skip(1));
    }

    protected override async Task PublishActivityEventsAsync(IRoutingSlip routingSlip, RoutingSlipBuilder builder,
        CancellationToken cancellationToken)
    {
        await base.PublishActivityEventsAsync(routingSlip, builder, cancellationToken).ConfigureAwait(false);

        await Publisher.PublishRoutingSlipRevisedAsync(Context.ActivityName, Context.ExecutionId, Context.Timestamp, Duration, routingSlip.Variables,
            routingSlip.Itinerary, builder.SourceItinerary, cancellationToken).ConfigureAwait(false);
    }
}
