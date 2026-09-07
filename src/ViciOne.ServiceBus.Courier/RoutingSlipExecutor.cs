using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Executes routing slip operations.</summary>
public sealed class RoutingSlipExecutor :
    IRoutingSlipExecutor
{
    readonly IPublishEndpoint _publishEndpoint;
    readonly ISendEndpointProvider _sendEndpointProvider;
    readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="sendEndpointProvider">The send endpoint provider.</param>
    /// <param name="publishEndpoint">The publish endpoint.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public RoutingSlipExecutor(ISendEndpointProvider sendEndpointProvider, IPublishEndpoint publishEndpoint, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(sendEndpointProvider);
        ArgumentNullException.ThrowIfNull(publishEndpoint);

        _sendEndpointProvider = sendEndpointProvider;
        _publishEndpoint = publishEndpoint;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="routingSlip">The routing slip.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync(RoutingSlip routingSlip, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(routingSlip);

        if (routingSlip.RanToCompletion())
        {
            var timestamp = _timeProvider.GetUtcNow().UtcDateTime;
            var duration = timestamp - routingSlip.CreateTimestamp;

            IRoutingSlipEventPublisher publisher = new RoutingSlipEventPublisher(_sendEndpointProvider, _publishEndpoint, routingSlip);

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
