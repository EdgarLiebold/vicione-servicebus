using System;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Resolves response and fault endpoints from a consume context.</summary>
public static class ConsumeContextEndpointExtensions
{
    /// <summary>Returns the endpoint for a fault, either directly to the requester or published.</summary>
    /// <typeparam name="T">The failed message contract.</typeparam>
    /// <param name="context">The consumed message whose fault routing metadata is used.</param>
    /// <param name="cancellationToken">Cancels endpoint resolution.</param>
    /// <returns>A task containing the resolved fault endpoint.</returns>
    public static Task<ISendEndpoint> GetFaultEndpointAsync<T>(this ConsumeContext context, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ISendEndpoint>(cancellationToken);

        var destinationAddress = context.FaultAddress ?? context.ResponseAddress;

        return GetEndpointAsync<Fault<T>>(
            context.Advanced().ReceiveContext,
            context,
            destinationAddress,
            context.RequestId,
            cancellationToken);
    }

    /// <summary>Returns the endpoint for a fault, either directly to the requester or published.</summary>
    /// <typeparam name="T">The fault contract.</typeparam>
    /// <param name="context">The consumed message whose metadata is propagated.</param>
    /// <param name="faultAddress">The explicit fault destination.</param>
    /// <param name="requestId">The request identifier assigned to the fault, or the consumed request identifier when omitted.</param>
    /// <param name="cancellationToken">Cancels endpoint resolution.</param>
    /// <returns>A task containing the resolved fault endpoint.</returns>
    public static Task<ISendEndpoint> GetFaultEndpointAsync<T>(this ConsumeContext context, Uri faultAddress, Guid? requestId = null, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(faultAddress);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ISendEndpoint>(cancellationToken);

        return GetEndpointAsync<T>(
            context.Advanced().ReceiveContext,
            context,
            faultAddress,
            requestId ?? context.RequestId,
            cancellationToken);
    }

    /// <summary>Returns the endpoint for a <see cref="ReceiveFault"/>, either directly to the requester or published.</summary>
    /// <param name="context">The transport receive context whose endpoint providers are used.</param>
    /// <param name="consumeContext">The consumed message whose routing metadata is propagated, or <see langword="null" />.</param>
    /// <param name="requestId">The request identifier assigned to the receive fault.</param>
    /// <param name="cancellationToken">Cancels endpoint resolution.</param>
    /// <returns>A task containing the resolved receive-fault endpoint.</returns>
    public static Task<ISendEndpoint> GetReceiveFaultEndpointAsync(this ReceiveContext context, ConsumeContext? consumeContext, Guid? requestId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ISendEndpoint>(cancellationToken);

        var destinationAddress = consumeContext?.FaultAddress ?? consumeContext?.ResponseAddress;

        return GetEndpointAsync<ReceiveFault>(context, consumeContext, destinationAddress, requestId, cancellationToken);
    }

    /// <summary>Returns the endpoint for a response, either directly to the requester or published.</summary>
    /// <typeparam name="T">The response contract.</typeparam>
    /// <param name="context">The consumed request whose response routing metadata is used.</param>
    /// <param name="cancellationToken">Cancels endpoint resolution.</param>
    /// <returns>A task containing the resolved response endpoint.</returns>
    public static Task<ISendEndpoint> GetResponseEndpointAsync<T>(this ConsumeContext context, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ISendEndpoint>(cancellationToken);

        return GetEndpointAsync<T>(
            context.Advanced().ReceiveContext,
            context,
            context.ResponseAddress,
            context.RequestId,
            cancellationToken);
    }

    /// <summary>Returns the endpoint for a response, either directly to the requester or published.</summary>
    /// <typeparam name="T">The response contract.</typeparam>
    /// <param name="context">The consumed request whose metadata is propagated.</param>
    /// <param name="responseAddress">The explicit response destination.</param>
    /// <param name="requestId">The request identifier assigned to the response, or the consumed request identifier when omitted.</param>
    /// <param name="cancellationToken">Cancels endpoint resolution.</param>
    /// <returns>A task containing the resolved response endpoint.</returns>
    public static Task<ISendEndpoint> GetResponseEndpointAsync<T>(this ConsumeContext context, Uri responseAddress, Guid? requestId = null, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(responseAddress);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ISendEndpoint>(cancellationToken);

        return GetEndpointAsync<T>(
            context.Advanced().ReceiveContext,
            context,
            responseAddress,
            requestId ?? context.RequestId,
            cancellationToken);
    }

    /// <summary>Returns the endpoint for a response, either directly to the requester or published.</summary>
    /// <typeparam name="T">The outgoing response or fault contract.</typeparam>
    /// <param name="receiveContext">The transport receive context whose endpoint providers are used.</param>
    /// <param name="consumeContext">The consumed message whose metadata is propagated, or <see langword="null" />.</param>
    /// <param name="destinationAddress">The explicit destination, or <see langword="null" /> to publish.</param>
    /// <param name="requestId">The request identifier assigned to the outgoing message.</param>
    /// <param name="cancellationToken">Cancels endpoint resolution.</param>
    /// <returns>A task containing the resolved endpoint.</returns>
    static Task<ISendEndpoint> GetEndpointAsync<T>(ReceiveContext receiveContext, ConsumeContext? consumeContext, Uri? destinationAddress,
        Guid? requestId, CancellationToken cancellationToken)
        where T : class
    {
        // This resolver is used only for responses and faults. Mark that semantic role explicitly:
        // destination-address matching cannot identify custom fault addresses or publish fallbacks,
        // and request deadlines must not leak into ordinary consume-context sends.
        if (destinationAddress != null && consumeContext != null)
            return GetSendEndpointAsync(
                receiveContext.SendEndpointProvider,
                consumeContext,
                destinationAddress,
                requestId,
                true,
                cancellationToken);

        return GetPublishEndpointAsync<T>(
            receiveContext.PublishEndpointProvider,
            consumeContext,
            requestId,
            true,
            cancellationToken);
    }

    internal static Task<ISendEndpoint> GetPublishEndpointAsync<T>(this IPublishEndpointProvider publishEndpointProvider, ConsumeContext? consumeContext,
        Guid? requestId, bool inheritRequestTimeToLive = false, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(publishEndpointProvider);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ISendEndpoint>(cancellationToken);

        Task<ISendEndpoint> publishSendEndpointTask = publishEndpointProvider.GetPublishSendEndpointAsync<T>(cancellationToken);
        if (publishSendEndpointTask.IsCompletedSuccessfully)
        {
            return consumeContext != null
                ? Task.FromResult<ISendEndpoint>(new ConsumeSendEndpoint(
                    publishSendEndpointTask.Result,
                    consumeContext,
                    requestId,
                    inheritRequestTimeToLive))
                : publishSendEndpointTask;
        }

        async Task<ISendEndpoint> ResolvePublishEndpointAsync()
        {
            var publishSendEndpoint = await publishSendEndpointTask.ConfigureAwait(false);

            return consumeContext != null
                ? new ConsumeSendEndpoint(publishSendEndpoint, consumeContext, requestId, inheritRequestTimeToLive)
                : publishSendEndpoint;
        }

        return ResolvePublishEndpointAsync();
    }

    internal static Task<ISendEndpoint> GetSendEndpointAsync(this ISendEndpointProvider sendEndpointProvider, ConsumeContext consumeContext,
        Uri destinationAddress, Guid? requestId, bool inheritRequestTimeToLive = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sendEndpointProvider);
        ArgumentNullException.ThrowIfNull(consumeContext);
        ArgumentNullException.ThrowIfNull(destinationAddress);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ISendEndpoint>(cancellationToken);

        Task<ISendEndpoint> sendEndpointTask = sendEndpointProvider.GetSendEndpointAsync(destinationAddress, cancellationToken);
        if (sendEndpointTask.IsCompletedSuccessfully)
        {
            return Task.FromResult<ISendEndpoint>(new ConsumeSendEndpoint(
                sendEndpointTask.Result,
                consumeContext,
                requestId,
                inheritRequestTimeToLive));
        }

        async Task<ISendEndpoint> ResolveSendEndpointAsync()
        {
            var sendEndpoint = await sendEndpointTask.ConfigureAwait(false);

            return new ConsumeSendEndpoint(sendEndpoint, consumeContext, requestId, inheritRequestTimeToLive);
        }

        return ResolveSendEndpointAsync();
    }

    internal static async Task GenerateFaultAsync<T>(this ConsumeContext<T> context, Exception exception)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exception);

        if (context.Advanced().ReceiveContext.PublishFaults || context.FaultAddress != null || context.ResponseAddress != null)
        {
            Fault<T> fault = new FaultEvent<T>(context.Message, context.MessageId, HostMetadataCache.Host, exception,
                context.Advanced().SupportedMessageTypes.ToArray(), context.GetTimeProvider());

            var faultPipe = new FaultPipe<T>(context);

            var faultContext = InternalOutboxExtensions.SkipOutbox(context.Advanced());

            var faultEndpoint = await faultContext.GetFaultEndpointAsync<T>(context.CancellationToken).ConfigureAwait(false);

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
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task SendAsync(SendContext<Fault<T>> context)
        {
            ArgumentNullException.ThrowIfNull(context);

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
            ArgumentNullException.ThrowIfNull(context);
            // Header projection has no configurable probe data.
        }
    }
}
