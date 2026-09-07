using System;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Results;

class TerminateExecutionResult<TArguments> :
    CompletedExecutionResult<TArguments>
    where TArguments : class
{
    public TerminateExecutionResult(ExecuteContext<TArguments> context, IRoutingSlipEventPublisher publisher, Activity activity, RoutingSlip routingSlip,
        Uri? compensationAddress)
        : base(context, publisher, activity, routingSlip, compensationAddress)
    {
    }

    protected override RoutingSlipBuilder CreateRoutingSlipBuilder(RoutingSlip routingSlip)
    {
        return new RoutingSlipBuilder(routingSlip, [], routingSlip.Itinerary.Skip(1));
    }

    protected override async Task PublishActivityEventsAsync(RoutingSlip routingSlip, RoutingSlipBuilder builder,
        CancellationToken cancellationToken)
    {
        await base.PublishActivityEventsAsync(routingSlip, builder, cancellationToken).ConfigureAwait(false);

        await Publisher.PublishRoutingSlipTerminatedAsync(Context.ActivityName, Context.ExecutionId, Context.Timestamp, Context.Elapsed, routingSlip.Variables,
            builder.SourceItinerary, cancellationToken).ConfigureAwait(false);
    }
}
