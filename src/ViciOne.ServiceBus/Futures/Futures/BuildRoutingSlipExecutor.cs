// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Futures
{
    using System.Threading.Tasks;
    using Courier.Contracts;


    public class BuildRoutingSlipExecutor<TInput> :
        IRoutingSlipExecutor<TInput>
        where TInput : class
    {
        readonly BuildItineraryCallback<TInput> _buildItinerary;

        public BuildRoutingSlipExecutor(BuildItineraryCallback<TInput> buildItinerary)
        {
            _buildItinerary = buildItinerary;
        }

        public async Task Execute(BehaviorContext<FutureState, TInput> context)
        {
            var trackingNumber = NewId.NextGuid();

            var builder = new RoutingSlipBuilder(trackingNumber);

            builder.AddVariable(MessageHeaders.FutureId, context.CorrelationId);

            builder.AddSubscription(context.ReceiveContext.InputAddress, RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

            await _buildItinerary(context, builder).ConfigureAwait(false);

            var routingSlip = builder.Build();

            await context.Execute(routingSlip).ConfigureAwait(false);

            if (TrackRoutingSlip)
                context.Saga.Pending.Add(trackingNumber);
        }

        public bool TrackRoutingSlip { get; set; }
    }
}
