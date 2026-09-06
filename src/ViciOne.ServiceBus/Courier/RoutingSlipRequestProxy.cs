using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Forwards routing slip request operations to an underlying context.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
public abstract class RoutingSlipRequestProxy<TRequest> :
    IConsumer<TRequest>
    where TRequest : class
{
    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual async Task ConsumeAsync(ConsumeContext<TRequest> context)
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid());

        builder.AddSubscription(GetResponseEndpointAddress(context), RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);

        builder.AddVariable(RoutingSlipRequestVariableNames.RequestId, context.RequestId);
        builder.AddVariable(RoutingSlipRequestVariableNames.ResponseAddress, context.ResponseAddress);
        builder.AddVariable(RoutingSlipRequestVariableNames.FaultAddress, context.FaultAddress);
        builder.AddVariable(RoutingSlipRequestVariableNames.Request, context.Message);
        builder.AddVariable(RoutingSlipRequestVariableNames.RequestAddress, context.Advanced().ReceiveContext.InputAddress);

        var retryAttempt = context.Headers.Get<int>(MessageHeaders.Request.RoutingSlipRetryCount);
        if (retryAttempt > 0)
            builder.AddVariable(RoutingSlipRequestVariableNames.RetryAttempt, retryAttempt);

        await BuildRoutingSlipAsync(builder, context);

        var routingSlip = builder.Build();

        await context.Advanced().ExecuteAsync(routingSlip, context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>Builds routing slip.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    /// <param name="request">The request.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected abstract Task BuildRoutingSlipAsync(RoutingSlipBuilder builder, ConsumeContext<TRequest> request);

    /// <summary>
    /// By default, returns the input address of the request consumer which assumes the response consumer is on the same receive endpoint.
    /// Override to specify the endpoint address of the response consumer if it is configured on a separate receive endpoint.
    /// </summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The response endpoint address.</returns>
    protected virtual Uri GetResponseEndpointAddress(ConsumeContext<TRequest> context)
    {
        return context.Advanced().ReceiveContext.InputAddress;
    }
}
