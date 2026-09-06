using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.Middleware;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Declares a move destination and copies a received message into a new Amazon SQS request.</summary>
/// <typeparam name="TSettings">The destination entity-settings type.</typeparam>
public class SqsMoveTransport<TSettings>
    where TSettings : class
{
    readonly string _destination;
    readonly bool _isFifo;
    readonly ConfigureAmazonSqsTopologyFilter<TSettings> _topologyFilter;

    /// <summary>Initializes an Amazon SQS move transport.</summary>
    /// <param name="destination">The logical destination queue name.</param>
    /// <param name="topologyFilter">The filter that declares the destination topology.</param>
    protected SqsMoveTransport(string destination, ConfigureAmazonSqsTopologyFilter<TSettings> topologyFilter)
    {
        _destination = destination;
        _topologyFilter = topologyFilter;

        _isFifo = AmazonSqsEndpointAddress.IsFifo(destination);
    }

    /// <summary>Copies a received message to the destination, preserving custom and FIFO attributes.</summary>
    /// <param name="context">The received message context.</param>
    /// <param name="preSend">The callback that adds move-specific headers before sending.</param>
    /// <param name="cancellationToken">The caller token used to cancel topology declaration and provider submission.</param>
    /// <returns>A task that completes when the destination accepts the message.</returns>
    protected async Task MoveAsync(ReceiveContext context, Action<SendMessageBatchRequestEntry, IDictionary<string, MessageAttributeValue>> preSend,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(preSend);
        cancellationToken.ThrowIfCancellationRequested();

        using var moveLifetime = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, cancellationToken);
        CancellationToken operationToken = moveLifetime.Token;

        if (!context.TryGetPayload(out ClientContext? clientContext))
            throw new ArgumentException("The ReceiveContext must contain a ClientContext (from Amazon SQS)", nameof(context));

        OneTimeContext<ConfigureTopologyContext<TSettings>> oneTimeContext =
            await _topologyFilter.ConfigureAsync(clientContext, operationToken).ConfigureAwait(false);

        operationToken.ThrowIfCancellationRequested();

        var message = new SendMessageBatchRequestEntry("", context.Body.GetString()) { MessageAttributes = new Dictionary<string, MessageAttributeValue>() };

        if (context.TryGetPayload(out AmazonSqsMessageContext? receiveContext))
        {
            if (_isFifo)
            {
                if (receiveContext.TransportMessage.Attributes != null)
                {
                    if (receiveContext.TransportMessage.Attributes.TryGetValue(MessageSystemAttributeName.MessageGroupId, out var messageGroupId)
                        && !string.IsNullOrWhiteSpace(messageGroupId))
                        message.MessageGroupId = messageGroupId;
                    if (receiveContext.TransportMessage.Attributes.TryGetValue(MessageSystemAttributeName.MessageDeduplicationId,
                            out var messageDeduplicationId)
                        && !string.IsNullOrWhiteSpace(messageDeduplicationId))
                        message.MessageDeduplicationId = messageDeduplicationId;
                }
            }

            CopyReceivedMessageHeaders(receiveContext, message.MessageAttributes);
        }

        preSend(message, message.MessageAttributes);

        try
        {
            await clientContext.SendMessageAsync(_destination, message, operationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            oneTimeContext.Evict();
            throw;
        }
    }

    static void CopyReceivedMessageHeaders(AmazonSqsMessageContext context, IDictionary<string, MessageAttributeValue> attributes)
    {
        foreach (var key in context.Attributes.Keys.Where(key => !key.StartsWith(MessageHeaders.Prefix, StringComparison.Ordinal)))
            attributes[key] = context.Attributes[key];
    }
}
