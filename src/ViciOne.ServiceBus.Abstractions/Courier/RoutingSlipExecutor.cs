using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier;

public class RoutingSlipExecutor :
    IRoutingSlipExecutor
{
    readonly IPublishEndpoint _publishEndpoint;
    readonly ISendEndpointProvider _sendEndpointProvider;
    readonly TimeProvider _timeProvider;

    public RoutingSlipExecutor(ISendEndpointProvider sendEndpointProvider, IPublishEndpoint publishEndpoint, TimeProvider? timeProvider = null)
    {
        _sendEndpointProvider = sendEndpointProvider;
        _publishEndpoint = publishEndpoint;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task Execute(RoutingSlip routingSlip, CancellationToken cancellationToken = default)
    {
        if (routingSlip.RanToCompletion())
        {
            var timestamp = _timeProvider.GetUtcNow().UtcDateTime;
            var duration = timestamp - routingSlip.CreateTimestamp;

            IRoutingSlipEventPublisher publisher = new RoutingSlipEventPublisher(_sendEndpointProvider, _publishEndpoint, routingSlip, cancellationToken);

            await publisher.PublishRoutingSlipCompleted(timestamp, duration, routingSlip.Variables).ConfigureAwait(false);
        }
        else
        {
            var address = routingSlip.GetNextExecuteAddress() ?? throw new RoutingSlipException("Activity execute address was not specified.");

            var endpoint = await _sendEndpointProvider.GetSendEndpoint(address).ConfigureAwait(false);

            await endpoint.Send(routingSlip, cancellationToken).ConfigureAwait(false);
        }
    }
}
