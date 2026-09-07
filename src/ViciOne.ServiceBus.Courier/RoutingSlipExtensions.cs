using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Provides routing-slip inspection and execution operations.</summary>
public static class RoutingSlipExtensions
{
    /// <summary>Determines whether the routing slip has no remaining activities.</summary>
    /// <param name="routingSlip">The routing slip.</param>
    /// <returns><see langword="true" /> when the itinerary is empty; otherwise, <see langword="false" />.</returns>
    public static bool RanToCompletion(this RoutingSlip routingSlip)
    {
        ArgumentNullException.ThrowIfNull(routingSlip);
        return routingSlip.Itinerary.Count == 0;
    }

    /// <summary>Gets the endpoint of the next activity, or <see langword="null" /> when the itinerary is empty.</summary>
    /// <param name="routingSlip">The routing slip.</param>
    /// <returns>The next execute address.</returns>
    public static Uri? GetNextExecuteAddress(this RoutingSlip routingSlip)
    {
        ArgumentNullException.ThrowIfNull(routingSlip);
        return routingSlip.Itinerary.Select(x => x.Address).FirstOrDefault();
    }

    /// <summary>Gets the endpoint of the next compensation, or <see langword="null" /> when no compensation remains.</summary>
    /// <param name="routingSlip">The routing slip.</param>
    /// <returns>The next compensate address.</returns>
    public static Uri? GetNextCompensateAddress(this RoutingSlip routingSlip)
    {
        ArgumentNullException.ThrowIfNull(routingSlip);
        return routingSlip.CompensateLogs.Select(x => x.Address).LastOrDefault();
    }

    /// <summary>Executes a routing slip through an endpoint that supports both sends and publishes.</summary>
    /// <typeparam name="T">The combined send and publish endpoint type.</typeparam>
    /// <param name="source">The endpoint used for routing and lifecycle events.</param>
    /// <param name="routingSlip">The routing slip.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the first activity or terminal event has been submitted.</returns>
    public static Task ExecuteAsync<T>(this T source, RoutingSlip routingSlip, CancellationToken cancellationToken = default)
        where T : IPublishEndpoint, ISendEndpointProvider
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(routingSlip);
        return new RoutingSlipExecutor(source, source).ExecuteAsync(routingSlip, cancellationToken);
    }

    /// <summary>
    /// Executes a routing slip through separate send and publish providers. The providers may be
    /// backed by a transactional bus outbox.
    /// </summary>
    /// <param name="routingSlip">The routing slip.</param>
    /// <param name="sendEndpointProvider">The send endpoint provider.</param>
    /// <param name="publishEndpoint">The publish endpoint.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the first activity or terminal event has been submitted.</returns>
    public static Task ExecuteAsync(this RoutingSlip routingSlip, ISendEndpointProvider sendEndpointProvider, IPublishEndpoint publishEndpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(routingSlip);
        ArgumentNullException.ThrowIfNull(sendEndpointProvider);
        ArgumentNullException.ThrowIfNull(publishEndpoint);

        return new RoutingSlipExecutor(sendEndpointProvider, publishEndpoint).ExecuteAsync(routingSlip, cancellationToken);
    }
}
