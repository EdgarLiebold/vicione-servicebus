using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Translates a request into a routing slip and subscribes the request endpoint to its terminal outcome.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
public abstract class RoutingSlipRequestProxy<TRequest> :
    IConsumer<TRequest>
    where TRequest : class
{
    /// <summary>Builds and submits a routing slip that retains the original request's response metadata.</summary>
    /// <param name="context">The request context whose transport identity and clock are preserved.</param>
    /// <returns>A task that completes after the routing slip has been accepted for execution.</returns>
    public virtual async Task ConsumeAsync(ConsumeContext<TRequest> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CancellationToken.ThrowIfCancellationRequested();

        Guid requestId = context.RequestId
            ?? throw new InvalidOperationException("A request identifier is required to correlate the routing-slip response.");
        TimeProvider timeProvider = context.GetTimeProvider();

        var builder = new RoutingSlipBuilder(NewId.NextGuid(), timeProvider);

        Uri responseAddress = context.ResponseAddress
            ?? throw new InvalidOperationException("A response address is required to return the routing-slip result to the requester.");
        Uri subscriptionEndpointAddress = GetResponseEndpointAddress(context)
            ?? throw new InvalidOperationException("The routing-slip event subscription endpoint address cannot be null.");
        builder.AddSubscription(subscriptionEndpointAddress, RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

        builder.SetVariable(RoutingSlipRequestVariableNames.RequestId, requestId);
        builder.SetVariable(RoutingSlipRequestVariableNames.ResponseAddress, responseAddress);
        builder.SetVariable(RoutingSlipRequestVariableNames.FaultAddress, context.FaultAddress);
        builder.SetVariable(RoutingSlipRequestVariableNames.Request, context.Message);
        builder.SetVariable(RoutingSlipRequestVariableNames.RequestAddress, context.Advanced().ReceiveContext.InputAddress);

        var retryAttempt = context.Headers.Get<int>(MessageHeaders.Request.RoutingSlipRetryCount);
        if (retryAttempt > 0)
            builder.SetVariable(RoutingSlipRequestVariableNames.RetryAttempt, retryAttempt);

        Task buildTask = BuildRoutingSlipAsync(builder, context)
            ?? throw new InvalidOperationException($"{nameof(ConsumeAsync)} cannot await a null task returned by {nameof(BuildRoutingSlipAsync)}.");
        await buildTask.ConfigureAwait(false);

        var routingSlip = builder.Build();

        ConsumeContext transportContext = context.Advanced();
        var executor = new RoutingSlipExecutor(transportContext, transportContext, timeProvider);
        await executor.ExecuteAsync(routingSlip, context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>Adds the request-specific itinerary and variables to the routing slip.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    /// <param name="request">The request context available to derived itinerary builders.</param>
    /// <returns>A non-null task that completes after request-specific routing-slip configuration.</returns>
    protected abstract Task BuildRoutingSlipAsync(RoutingSlipBuilder builder, ConsumeContext<TRequest> request);

    /// <summary>
    /// By default, returns the input address of the request consumer which assumes the response consumer is on the same receive endpoint.
    /// Override to specify the endpoint address of the response consumer if it is configured on a separate receive endpoint.
    /// </summary>
    /// <param name="context">The request context whose receive endpoint is used by default.</param>
    /// <returns>The endpoint that consumes terminal routing-slip events and creates the request response.</returns>
    protected virtual Uri GetResponseEndpointAddress(ConsumeContext<TRequest> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Advanced().ReceiveContext.InputAddress;
    }
}
