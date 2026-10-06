using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Logging.Monitoring;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Executes the pipeline for outbox message.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class OutboxMessagePipe<TMessage> :
    IPipe<OutboxConsumeContext<TMessage>>
    where TMessage : class
{
    readonly IPipe<ConsumeContext<TMessage>> _next;
    readonly OutboxConsumeOptions _options;
    readonly IConsumeScopeContext<TMessage> _scopeContext;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="scopeContext">The scope context.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    public OutboxMessagePipe(OutboxConsumeOptions options, IConsumeScopeContext<TMessage> scopeContext, IPipe<ConsumeContext<TMessage>> next)
    {
        _options = options;
        _scopeContext = scopeContext;
        _next = next;
    }

    /// <summary>Processes inline consumption and provider-controlled outbox delivery for the supplied context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(OutboxConsumeContext<TMessage> context)
    {
        var pop = _scopeContext.PushConsumeContext(context);
        Exception? operationFailure = null;
        try
        {
            TimeProvider timeProvider = context.GetTimeProvider();
            long startedAt = timeProvider.GetTimestamp();

            if (!context.IsMessageConsumed)
            {
                await _next.SendAsync(context).ConfigureAwait(false);

                await context.ConsumeCompleted.ConfigureAwait(false);

                try
                {
                    await context.SetConsumedAsync().ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    if (!context.ReceiveContext.IsFaulted)
                        await ConsumerIngressFailure.NotifyFaultedAsync(context, timeProvider.GetElapsedTime(startedAt), TypeCache<TMessage>.ShortName,
                            exception, exception).ConfigureAwait(false);

                    throw;
                }

                return;
            }

            if (!context.IsOutboxDelivered)
            {
                await DeliverOutboxMessagesAsync(context).ConfigureAwait(false);

                await context.ConsumeCompleted.ConfigureAwait(false);

                return;
            }

            await context.RemoveOutboxMessagesAsync().ConfigureAwait(false);

            try
            {
                LogContext.Debug?.Log("Outbox Completed: {MessageId} ({ReceiveCount})", context.MessageId, context.ReceiveCount);
            }
            catch (Exception)
            {
            }

            if (context.ReceiveContext is { IsDelivered: false, IsFaulted: false })
                await context.NotifyConsumedAsync(context, timeProvider.GetElapsedTime(startedAt), _options.ConsumerType).ConfigureAwait(false);

            context.ContinueProcessing = false;
        }
        catch (Exception exception)
        {
            operationFailure = exception;
            throw;
        }
        finally
        {
            try
            {
                pop?.Dispose();
            }
            catch (Exception restoreFailure) when (operationFailure is not null)
            {
                throw new AggregateException(
                    "Outbox pipeline and ambient context restoration encountered multiple failures.",
                    operationFailure,
                    restoreFailure);
            }
        }
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("outbox");

        _next.Probe(scope);
    }

    async Task DeliverOutboxMessagesAsync(OutboxConsumeContext context)
    {
        List<OutboxMessageContext> messages = await context.LoadOutboxMessagesAsync().ConfigureAwait(false);

        var messageLimit = _options.MessageDeliveryLimit;
        var messageCount = 0;
        var messageIndex = 0;
        for (; messageIndex < messages.Count && messageCount < messageLimit; messageIndex++)
        {
            var message = messages[messageIndex];

            if (context.LastSequenceNumber != null && context.LastSequenceNumber >= message.SequenceNumber)
            {
            }
            else if (message.DestinationAddress == null)
            {
                throw new InvalidOperationException(
                    $"Outbox message DestinationAddress not present: {message.SequenceNumber} {message.MessageId}");
            }
            else
            {
                using var sendToken = new CancellationTokenSource(_options.MessageDeliveryTimeout);
                using var token = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, sendToken.Token);

                var pipe = new OutboxMessageSendPipe(message, message.DestinationAddress);

                var endpoint = await context.CapturedContext.GetSendEndpointAsync(message.DestinationAddress, cancellationToken: token.Token)
                    .ConfigureAwait(false);

                // Captured messages already have an outbox owner. Delivery must await the transport,
                // rather than capture them again in an enclosing volatile outbox before recording progress.
                ISendEndpoint deliveryEndpoint = endpoint is ConsumeSendEndpoint consumeEndpoint ? consumeEndpoint.Endpoint : endpoint;
                ISendEndpoint capturedEndpoint = deliveryEndpoint;
                while (deliveryEndpoint is InMemoryOutbox.OutboxSendEndpoint volatileEndpoint)
                    deliveryEndpoint = volatileEndpoint.Endpoint;
                if (!ReferenceEquals(deliveryEndpoint, capturedEndpoint))
                    endpoint = endpoint is ConsumeSendEndpoint consume ? consume.WithEndpoint(deliveryEndpoint) : deliveryEndpoint;

                StartedActivity? activity = MessageActivity.TryStartOutboxDelivery(message);
                MetricOperation? instrument = LogContext.Current?.TryStartOutboxDeliveryMetrics();
                try
                {
                    await endpoint.SendAsync(SerializedTransportMessage.Instance, pipe, token.Token).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    activity?.AddExceptionEvent(exception);
                    instrument?.RecordException(exception);

                    throw;
                }
                finally
                {
                    activity?.Stop();
                    instrument?.Complete();
                }

                try
                {
                    LogContext.Debug?.Log("Outbox Sent: {InboxMessageId} {SequenceNumber} {MessageId}", context.MessageId, message.SequenceNumber,
                        message.MessageId);
                }
                catch (Exception)
                {
                }

                await context.NotifyOutboxMessageDeliveredAsync(message).ConfigureAwait(false);

                messageCount++;
            }
        }

        if (messageIndex == messages.Count && messages.Count < messageLimit)
            await context.SetDeliveredAsync().ConfigureAwait(false);
    }
}
