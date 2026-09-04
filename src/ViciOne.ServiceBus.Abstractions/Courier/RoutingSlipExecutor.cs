using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier;

/// <summary>
/// Provides a routing slip executor implementation.
/// </summary>
public class RoutingSlipExecutor :
    IRoutingSlipExecutor
{
    readonly IPublishEndpoint _publishEndpoint;
    readonly ISendEndpointProvider _sendEndpointProvider;
    readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="sendEndpointProvider">The send endpoint provider value.</param>
    /// <param name="publishEndpoint">The publish endpoint value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public RoutingSlipExecutor(ISendEndpointProvider sendEndpointProvider, IPublishEndpoint publishEndpoint, TimeProvider? timeProvider = null)
    {
        _sendEndpointProvider = sendEndpointProvider;
        _publishEndpoint = publishEndpoint;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="routingSlip">The routing slip value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ExecuteAsync(RoutingSlip routingSlip, CancellationToken cancellationToken = default)
    {
        if (routingSlip.RanToCompletion())
        {
            var timestamp = _timeProvider.GetUtcNow().UtcDateTime;
            var duration = timestamp - routingSlip.CreateTimestamp;

            IRoutingSlipEventPublisher publisher = new RoutingSlipEventPublisher(_sendEndpointProvider, _publishEndpoint, routingSlip, cancellationToken);

            await publisher.PublishRoutingSlipCompletedAsync(timestamp, duration, routingSlip.Variables, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var address = routingSlip.GetNextExecuteAddress() ?? throw new RoutingSlipException("Activity execute address was not specified.");

            var endpoint = await _sendEndpointProvider.GetSendEndpointAsync(address, cancellationToken: cancellationToken).ConfigureAwait(false);

            await endpoint.SendAsync(routingSlip, cancellationToken).ConfigureAwait(false);
        }
    }
}
