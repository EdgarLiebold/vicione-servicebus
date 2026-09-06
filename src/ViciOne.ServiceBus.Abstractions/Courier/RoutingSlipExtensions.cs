using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Provides extension methods for routing slip.</summary>
public static class RoutingSlipExtensions
{
    /// <summary>Returns true if there are no remaining activities to be executed.</summary>
    /// <param name="routingSlip">The routing slip.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool RanToCompletion(this RoutingSlip routingSlip)
    {
        return routingSlip.Itinerary.Count == 0;
    }

    /// <summary>Gets next execute address.</summary>
    /// <param name="routingSlip">The routing slip.</param>
    /// <returns>The next execute address.</returns>
    public static Uri? GetNextExecuteAddress(this RoutingSlip routingSlip)
    {
        return routingSlip.Itinerary.Select(x => x.Address).First();
    }

    /// <summary>Gets next compensate address.</summary>
    /// <param name="routingSlip">The routing slip.</param>
    /// <returns>The next compensate address.</returns>
    public static Uri? GetNextCompensateAddress(this RoutingSlip routingSlip)
    {
        return routingSlip.CompensateLogs.Select(x => x.Address).Last();
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="routingSlip">The routing slip.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task ExecuteAsync<T>(this T source, RoutingSlip routingSlip, CancellationToken cancellationToken = default)
        where T : IPublishEndpoint, ISendEndpointProvider
    {
        return new RoutingSlipExecutor(source, source).ExecuteAsync(routingSlip, cancellationToken);
    }

    /// <summary>
    /// Execute a routing slip via the <paramref name="sendEndpointProvider"/> and/or <paramref name="publishEndpoint"/> provided.
    /// This method works with the bus outbox (from the transactional outbox).
    /// </summary>
    /// <param name="routingSlip">The routing slip.</param>
    /// <param name="sendEndpointProvider">The send endpoint provider.</param>
    /// <param name="publishEndpoint">The publish endpoint.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task ExecuteAsync(this RoutingSlip routingSlip, ISendEndpointProvider sendEndpointProvider, IPublishEndpoint publishEndpoint,
        CancellationToken cancellationToken = default)
    {
        if (routingSlip == null)
            throw new ArgumentNullException(nameof(routingSlip));
        if (sendEndpointProvider == null)
            throw new ArgumentNullException(nameof(sendEndpointProvider));
        if (publishEndpoint == null)
            throw new ArgumentNullException(nameof(publishEndpoint));

        return new RoutingSlipExecutor(sendEndpointProvider, publishEndpoint).ExecuteAsync(routingSlip, cancellationToken);
    }
}
