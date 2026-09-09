using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Events;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Translates terminal routing-slip events into responses for the original request.</summary>
/// <typeparam name="TRequest">The original request contract.</typeparam>
/// <typeparam name="TResponse">The successful response contract.</typeparam>
/// <typeparam name="TFault">The terminal fault response contract.</typeparam>
public abstract class RoutingSlipResponseProxy<TRequest, TResponse, TFault> :
    IConsumer<RoutingSlipCompleted>,
    IConsumer<RoutingSlipFaulted>
    where TRequest : class
    where TResponse : class
    where TFault : class
{
    /// <summary>Gets the optional policy used to retry the original request after routing-slip failure.</summary>
    protected virtual IRetryPolicy? RetryPolicy => null;

    /// <inheritdoc />
    public virtual async Task ConsumeAsync(ConsumeContext<RoutingSlipCompleted> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var requestInfo = new RoutingSlipRequestInfo<TRequest>(context.Advanced().SerializerContext, context.Message.Variables);

        var endpoint = await context.Advanced()
            .GetResponseEndpointAsync<TResponse>(requestInfo.ResponseAddress, requestInfo.RequestId, context.CancellationToken)
            .ConfigureAwait(false);

        var response = await CreateResponseMessageAsync(context, requestInfo.Request).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The routing slip response proxy returned a null response.");

        await endpoint.SendAsync(response, context.CancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task ConsumeAsync(ConsumeContext<RoutingSlipFaulted> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var requestInfo = new RoutingSlipRequestInfo<TRequest>(context.Advanced().SerializerContext, context.Message.Variables);

        if (CanRetry(requestInfo, context, out var delay))
        {
            var retryAttempt = requestInfo.RetryAttempt ?? 0;
            var requestAddress = requestInfo.RequestAddress
                ?? throw new InvalidOperationException("The routing slip request address is required for a retry.");

            var schedulerContext = context.GetPayload<MessageSchedulerContext>();
            Action<SendContext<TRequest>> configureRetry = sendContext =>
            {
                sendContext.RequestId = requestInfo.RequestId;
                sendContext.ResponseAddress = requestInfo.ResponseAddress;
                sendContext.FaultAddress = requestInfo.FaultAddress;
                sendContext.Delay = delay;
                sendContext.Headers.Set(MessageHeaders.Request.RoutingSlipRetryCount, retryAttempt + 1);
            };

            await schedulerContext.ScheduleSendAsync(requestAddress, delay, requestInfo.Request, configureRetry.ToPipe(),
                context.CancellationToken).ConfigureAwait(false);

            return;
        }

        var endpoint = await context.Advanced()
            .GetFaultEndpointAsync<TRequest>(requestInfo.FaultAddress ?? requestInfo.ResponseAddress, requestInfo.RequestId, context.CancellationToken)
            .ConfigureAwait(false);

        var response = await CreateFaultedResponseMessageAsync(context, requestInfo.Request, requestInfo.RequestId).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The routing slip response proxy returned a null fault response.");

        await endpoint.SendAsync(response, x =>
        {
            if (requestInfo.RetryAttempt > 0)
                x.Headers.Set(MessageHeaders.FaultRetryCount, requestInfo.RetryAttempt.Value);
        }, context.CancellationToken).ConfigureAwait(false);
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

    /// <summary>Creates the response sent after successful routing-slip completion.</summary>
    /// <param name="context">The routing-slip completion context.</param>
    /// <param name="request">The original request.</param>
    /// <returns>A task that produces the successful response.</returns>
    protected abstract Task<TResponse> CreateResponseMessageAsync(ConsumeContext<RoutingSlipCompleted> context, TRequest request);

    /// <summary>Creates the response sent after terminal routing-slip failure.</summary>
    /// <param name="context">The routing-slip fault context.</param>
    /// <param name="request">The original request.</param>
    /// <param name="requestId">The original request identifier.</param>
    /// <returns>A task that produces the terminal fault response.</returns>
    protected abstract Task<TFault> CreateFaultedResponseMessageAsync(ConsumeContext<RoutingSlipFaulted> context, TRequest request, Guid requestId);
}


/// <summary>Translates terminal routing-slip events into a response or a standard request fault.</summary>
/// <typeparam name="TRequest">The original request contract.</typeparam>
/// <typeparam name="TResponse">The successful response contract.</typeparam>
public abstract class RoutingSlipResponseProxy<TRequest, TResponse> :
    RoutingSlipResponseProxy<TRequest, TResponse, Fault<TRequest>>
    where TRequest : class
    where TResponse : class
{
    /// <inheritdoc />
    protected override Task<Fault<TRequest>> CreateFaultedResponseMessageAsync(ConsumeContext<RoutingSlipFaulted> context, TRequest request, Guid requestId)
    {
        IEnumerable<ExceptionInfo> exceptions = context.Message.ActivityExceptions.Select(x => x.ExceptionInfo);

        Fault<TRequest> response = new FaultEvent<TRequest>(request, requestId, context.Host, exceptions,
            MessageTypeCache<TRequest>.MessageTypeNames.ToArray(),
            context.GetTimeProvider());

        return Task.FromResult(response);
    }
}
