using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Messages;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Validates and submits routing slips to their next activity or publishes terminal completion.</summary>
public sealed class RoutingSlipExecutor :
    IRoutingSlipExecutor
{
    readonly IPublishEndpoint _publishEndpoint;
    readonly ISendEndpointProvider _sendEndpointProvider;
    readonly TimeProvider _timeProvider;

    /// <summary>Creates an executor from the transport capabilities used for activity submission and lifecycle publication.</summary>
    /// <param name="sendEndpointProvider">The provider that resolves activity endpoints.</param>
    /// <param name="publishEndpoint">The endpoint that publishes completion events.</param>
    /// <param name="timeProvider">The clock used to timestamp terminal completion.</param>
    public RoutingSlipExecutor(ISendEndpointProvider sendEndpointProvider, IPublishEndpoint publishEndpoint, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(sendEndpointProvider);
        ArgumentNullException.ThrowIfNull(publishEndpoint);

        _sendEndpointProvider = sendEndpointProvider;
        _publishEndpoint = publishEndpoint;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Submits an isolated routing-slip snapshot or publishes completion when its itinerary is empty.</summary>
    /// <param name="routingSlip">The routing slip to validate and submit.</param>
    /// <param name="cancellationToken">The token that cancels validation and transport submission.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    public async Task ExecuteAsync(IRoutingSlip routingSlip, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(routingSlip);
        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset timestamp = _timeProvider.GetUtcNow();
        RoutingSlipRoutingSlip snapshot = CreateSnapshot(routingSlip, timestamp);

        if (snapshot.RanToCompletion())
        {
            var duration = timestamp - snapshot.CreateTimestamp;

            var publisher = new RoutingSlipEventPublisher(_sendEndpointProvider, _publishEndpoint, snapshot);

            await publisher.PublishRoutingSlipCompletedAsync(timestamp, duration, snapshot.Variables, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var address = snapshot.GetNextExecuteAddress() ?? throw new RoutingSlipException("Activity execute address was not specified.");

            Task<ISendEndpoint> endpointTask = _sendEndpointProvider.GetSendEndpointAsync(address, cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException($"The send endpoint provider returned no endpoint resolution task for '{address}'.");
            var endpoint = await endpointTask.ConfigureAwait(false)
                ?? throw new InvalidOperationException($"The send endpoint provider resolved no send endpoint for '{address}'.");

            cancellationToken.ThrowIfCancellationRequested();
            await endpoint.SendAsync(snapshot, cancellationToken).ConfigureAwait(false);
        }
    }

    static RoutingSlipRoutingSlip CreateSnapshot(IRoutingSlip routingSlip, DateTimeOffset now)
    {
        if (routingSlip.TrackingNumber == Guid.Empty)
            throw InvalidRoutingSlip("The routing-slip tracking number cannot be empty.", nameof(routingSlip));
        if (routingSlip.CreateTimestamp == default)
            throw InvalidRoutingSlip("The routing-slip creation timestamp is required.", nameof(routingSlip));
        if (routingSlip.CreateTimestamp > now)
            throw InvalidRoutingSlip("The routing-slip creation timestamp cannot be later than the submission time.", nameof(routingSlip));
        if (routingSlip.Itinerary is null
            || routingSlip.ActivityLogs is null
            || routingSlip.CompensateLogs is null
            || routingSlip.Variables is null
            || routingSlip.ActivityExceptions is null
            || routingSlip.Subscriptions is null)
        {
            throw InvalidRoutingSlip("Routing-slip collections cannot be null.", nameof(routingSlip));
        }

        try
        {
            return new RoutingSlipRoutingSlip(
                routingSlip.TrackingNumber,
                routingSlip.CreateTimestamp,
                routingSlip.Itinerary,
                routingSlip.ActivityLogs,
                routingSlip.CompensateLogs,
                routingSlip.ActivityExceptions,
                routingSlip.Variables,
                routingSlip.Subscriptions);
        }
        catch (Exception exception) when (exception is ArgumentException or SerializationException)
        {
            throw InvalidRoutingSlip("The routing slip contains invalid activity, log, failure, or subscription data.", nameof(routingSlip), exception);
        }
    }

    static ArgumentException InvalidRoutingSlip(string message, string parameterName, Exception? innerException = null) =>
        new(message, parameterName, innerException);
}
