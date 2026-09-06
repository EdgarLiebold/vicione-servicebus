using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Events;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Forwards routing slip response operations to an underlying context.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <typeparam name="TFault">The fault type.</typeparam>
public abstract class RoutingSlipResponseProxy<TRequest, TResponse, TFault> :
    IConsumer<RoutingSlipCompleted>,
    IConsumer<RoutingSlipFaulted>
    where TRequest : class
    where TResponse : class
    where TFault : class
{
    /// <summary>Gets the retry policy.</summary>
    protected virtual IRetryPolicy? RetryPolicy => null;

    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual async Task ConsumeAsync(ConsumeContext<RoutingSlipCompleted> context)
    {
        var requestInfo = new RoutingSlipRequestInfo<TRequest>(context.Advanced().SerializerContext, context.Message.Variables);

        var endpoint = await context.Advanced().GetResponseEndpointAsync<TResponse>(requestInfo.ResponseAddress, requestInfo.RequestId).ConfigureAwait(false);

        var response = await CreateResponseMessageAsync(context, requestInfo.Request).ConfigureAwait(false);

        await endpoint.SendAsync(response).ConfigureAwait(false);
    }

    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual async Task ConsumeAsync(ConsumeContext<RoutingSlipFaulted> context)
    {
        var requestInfo = new RoutingSlipRequestInfo<TRequest>(context.Advanced().SerializerContext, context.Message.Variables);

        if (CanRetry(requestInfo, context, out var delay))
        {
            var retryAttempt = requestInfo.RetryAttempt ?? 0;
            var requestAddress = requestInfo.RequestAddress
                ?? throw new InvalidOperationException("The routing slip request address is required for a retry.");

            var schedulerContext = context.GetPayload<MessageSchedulerContext>();

            await schedulerContext.ScheduleSendAsync(requestAddress, delay, requestInfo.Request, x =>
            {
                x.RequestId = requestInfo.RequestId;
                x.ResponseAddress = requestInfo.ResponseAddress;
                x.FaultAddress = requestInfo.FaultAddress;
                x.Delay = delay;
                x.Headers.Set(MessageHeaders.Request.RoutingSlipRetryCount, retryAttempt + 1);
            }).ConfigureAwait(false);

            return;
        }

        var endpoint = await context.Advanced().GetFaultEndpointAsync<TRequest>(requestInfo.FaultAddress ?? requestInfo.ResponseAddress, requestInfo.RequestId)
            .ConfigureAwait(false);

        var response = await CreateFaultedResponseMessageAsync(context, requestInfo.Request, requestInfo.RequestId);

        await endpoint.SendAsync(response, x =>
        {
            if (requestInfo.RetryAttempt > 0)
                x.Headers.Set(MessageHeaders.FaultRetryCount, requestInfo.RetryAttempt.Value);
        }).ConfigureAwait(false);
    }

    bool CanRetry(RoutingSlipRequestInfo<TRequest> requestInfo, ConsumeContext<RoutingSlipFaulted> context, out TimeSpan delay)
    {
        delay = default;

        var retryPolicy = RetryPolicy;
        if (retryPolicy == null)
            return false;

        RetryPolicyContext<ConsumeContext<RoutingSlipFaulted>> policyContext = retryPolicy.CreatePolicyContext(context);

        var exception = new RoutingSlipRequestFaultedException(context.Message);

        if (!policyContext.CanRetry(exception, out RetryContext<ConsumeContext<RoutingSlipFaulted>> retryContext))
            return false;

        var retryAttempt = requestInfo.RetryAttempt ?? 0;
        for (var retryIndex = 0; retryIndex < retryAttempt; retryIndex++)
        {
            if (!retryContext.CanRetry(exception, out retryContext))
                return false;
        }

        delay = retryContext.Delay ?? TimeSpan.Zero;
        return true;
    }

    /// <summary>Creates response message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="request">The request.</param>
    /// <returns>A task that produces the created value.</returns>
    protected abstract Task<TResponse> CreateResponseMessageAsync(ConsumeContext<RoutingSlipCompleted> context, TRequest request);

    /// <summary>Creates faulted response message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="request">The request.</param>
    /// <param name="requestId">The request id.</param>
    /// <returns>A task that produces the created value.</returns>
    protected abstract Task<TFault> CreateFaultedResponseMessageAsync(ConsumeContext<RoutingSlipFaulted> context, TRequest request, Guid requestId);
}


/// <summary>Forwards routing slip response operations to an underlying context.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public abstract class RoutingSlipResponseProxy<TRequest, TResponse> :
    RoutingSlipResponseProxy<TRequest, TResponse, Fault<TRequest>>
    where TRequest : class
    where TResponse : class
{
    /// <summary>Creates faulted response message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="request">The request.</param>
    /// <param name="requestId">The request id.</param>
    /// <returns>A task that produces the created value.</returns>
    protected override Task<Fault<TRequest>> CreateFaultedResponseMessageAsync(ConsumeContext<RoutingSlipFaulted> context, TRequest request, Guid requestId)
    {
        IEnumerable<ExceptionInfo> exceptions = context.Message.ActivityExceptions.Select(x => x.ExceptionInfo);

        Fault<TRequest> response = new FaultEvent<TRequest>(request, requestId, context.Host, exceptions,
            MessageTypeCache<TRequest>.MessageTypeNames.ToArray(),
            context.GetTimeProvider());

        return Task.FromResult(response);
    }
}
