using System;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus;

public static class ConsumeContextEndpointExtensions
{
    /// <summary>
    /// Returns the endpoint for a fault, either directly to the requester or published
    /// </summary>
    /// <param name="context"></param>
    /// <typeparam name="T">The message type</typeparam>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task<ISendEndpoint> GetFaultEndpointAsync<T>(this ConsumeContext context, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.ISendEndpoint>(cancellationToken); var destinationAddress = context.FaultAddress ?? context.ResponseAddress;

        return GetEndpointAsync<Fault<T>>(context.Advanced().ReceiveContext, context, destinationAddress, context.RequestId);
    }

    /// <summary>
    /// Returns the endpoint for a fault, either directly to the requester or published
    /// </summary>
    /// <param name="context"></param>
    /// <param name="faultAddress"></param>
    /// <param name="requestId"></param>
    /// <typeparam name="T">The response type</typeparam>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task<ISendEndpoint> GetFaultEndpointAsync<T>(this ConsumeContext context, Uri faultAddress, Guid? requestId = null, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.ISendEndpoint>(cancellationToken); var destinationAddress = faultAddress ?? context.FaultAddress ?? context.ResponseAddress;

        return GetEndpointAsync<T>(context.Advanced().ReceiveContext, context, destinationAddress, requestId ?? context.RequestId);
    }

    /// <summary>
    /// Returns the endpoint for a <see cref="ReceiveFault"/>, either directly to the requester or published
    /// </summary>
    /// <param name="context"></param>
    /// <param name="consumeContext"></param>
    /// <param name="requestId"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task<ISendEndpoint> GetReceiveFaultEndpointAsync(this ReceiveContext context, ConsumeContext? consumeContext, Guid? requestId, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.ISendEndpoint>(cancellationToken); var destinationAddress = consumeContext?.FaultAddress ?? consumeContext?.ResponseAddress;

        return GetEndpointAsync<ReceiveFault>(context, consumeContext, destinationAddress, requestId);
    }

    /// <summary>
    /// Returns the endpoint for a response, either directly to the requester or published
    /// </summary>
    /// <param name="context"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task<ISendEndpoint> GetResponseEndpointAsync<T>(this ConsumeContext context, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.ISendEndpoint>(cancellationToken); return GetEndpointAsync<T>(context.Advanced().ReceiveContext, context, context.ResponseAddress, context.RequestId);
    }

    /// <summary>
    /// Returns the endpoint for a response, either directly to the requester or published
    /// </summary>
    /// <param name="context"></param>
    /// <param name="responseAddress"></param>
    /// <param name="requestId"></param>
    /// <typeparam name="T">The response type</typeparam>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task<ISendEndpoint> GetResponseEndpointAsync<T>(this ConsumeContext context, Uri responseAddress, Guid? requestId = null, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.ISendEndpoint>(cancellationToken); return GetEndpointAsync<T>(context.Advanced().ReceiveContext, context, responseAddress ?? context.ResponseAddress, requestId ?? context.RequestId);
    }

    /// <summary>
    /// Returns the endpoint for a response, either directly to the requester or published
    /// </summary>
    /// <param name="receiveContext"></param>
    /// <param name="consumeContext"></param>
    /// <param name="destinationAddress"></param>
    /// <param name="requestId"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    static Task<ISendEndpoint> GetEndpointAsync<T>(ReceiveContext receiveContext, ConsumeContext? consumeContext, Uri? destinationAddress, Guid? requestId)
        where T : class
    {
        // This resolver is used only for responses and faults. Mark that semantic role explicitly:
        // destination-address matching cannot identify custom fault addresses or publish fallbacks,
        // and request deadlines must not leak into ordinary consume-context sends.
        if (destinationAddress != null && consumeContext != null)
            return GetSendEndpointAsync(receiveContext.SendEndpointProvider, consumeContext, destinationAddress, requestId, true);

        return GetPublishEndpointAsync<T>(receiveContext.PublishEndpointProvider, consumeContext, requestId, true);
    }

    internal static Task<ISendEndpoint> GetPublishEndpointAsync<T>(this IPublishEndpointProvider publishEndpointProvider, ConsumeContext? consumeContext,
        Guid? requestId, bool inheritRequestTimeToLive = false)
        where T : class
    {
        Task<ISendEndpoint> publishSendEndpointTask = publishEndpointProvider.GetPublishSendEndpointAsync<T>();
        if (publishSendEndpointTask.Status == TaskStatus.RanToCompletion)
        {
            return consumeContext != null
                ? Task.FromResult<ISendEndpoint>(new ConsumeSendEndpoint(
                    publishSendEndpointTask.Result,
                    consumeContext,
                    requestId,
                    inheritRequestTimeToLive))
                : publishSendEndpointTask;
        }

        async Task<ISendEndpoint> GetPublishSendEndpointAsync()
        {
            var publishSendEndpoint = await publishSendEndpointTask.ConfigureAwait(false);

            return consumeContext != null
                ? new ConsumeSendEndpoint(publishSendEndpoint, consumeContext, requestId, inheritRequestTimeToLive)
                : publishSendEndpoint;
        }

        return GetPublishSendEndpointAsync();
    }

    internal static Task<ISendEndpoint> GetSendEndpointAsync(this ISendEndpointProvider sendEndpointProvider, ConsumeContext consumeContext,
        Uri destinationAddress, Guid? requestId, bool inheritRequestTimeToLive = false)
    {
        Task<ISendEndpoint> sendEndpointTask = sendEndpointProvider.GetSendEndpointAsync(destinationAddress);
        if (sendEndpointTask.Status == TaskStatus.RanToCompletion)
        {
            return Task.FromResult<ISendEndpoint>(new ConsumeSendEndpoint(
                sendEndpointTask.Result,
                consumeContext,
                requestId,
                inheritRequestTimeToLive));
        }

        async Task<ISendEndpoint> GetResponseEndpointAsync()
        {
            var sendEndpoint = await sendEndpointTask.ConfigureAwait(false);

            return new ConsumeSendEndpoint(sendEndpoint, consumeContext, requestId, inheritRequestTimeToLive);
        }

        return GetResponseEndpointAsync();
    }

    internal static async Task GenerateFaultAsync<T>(this ConsumeContext<T> context, Exception exception)
        where T : class
    {
        if (context.Advanced().ReceiveContext.PublishFaults || context.FaultAddress != null || context.ResponseAddress != null)
        {
            Fault<T> fault = new FaultEvent<T>(context.Message, context.MessageId, HostMetadataCache.Host, exception,
                context.Advanced().SupportedMessageTypes.ToArray(), context.GetTimeProvider());

            var faultPipe = new FaultPipe<T>(context);

            var faultContext = InternalOutboxExtensions.SkipOutbox(context.Advanced());

            var faultEndpoint = await faultContext.GetFaultEndpointAsync<T>().ConfigureAwait(false);

            await faultEndpoint.SendAsync(fault, faultPipe, context.CancellationToken).ConfigureAwait(false);
        }
    }


    class FaultPipe<T> :
        IPipe<SendContext<Fault<T>>>
        where T : class
    {
        readonly ConsumeContext<T> _context;

        public FaultPipe(ConsumeContext<T> context)
        {
            _context = context;
        }

        public Task SendAsync(SendContext<Fault<T>> context)
        {
            context.TransferConsumeContextHeaders(_context.Advanced());

            context.CorrelationId = _context.CorrelationId;
            context.RequestId = _context.RequestId;

            if (_context.TryGetPayload(out ConsumeRetryContext? consumeRetryContext) && consumeRetryContext.RetryCount > 0)
                context.Headers.Set(MessageHeaders.FaultRetryCount, consumeRetryContext.RetryCount);
            else if (_context.TryGetPayload(out RetryContext? retryContext) && retryContext.RetryCount > 0)
                context.Headers.Set(MessageHeaders.FaultRetryCount, retryContext.RetryCount);

            var redeliveryCount = _context.Advanced().GetRedeliveryCount();
            if (redeliveryCount > 0)
                context.Headers.Set(MessageHeaders.FaultRedeliveryCount, redeliveryCount);

            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
        }
    }
}
