using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.Middleware;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides a sqs move transport implementation.
/// </summary>
/// <typeparam name="TSettings">The t settings type.</typeparam>
public class SqsMoveTransport<TSettings>
    where TSettings : class
{
    readonly string _destination;
    readonly bool _isFifo;
    readonly ConfigureAmazonSqsTopologyFilter<TSettings> _topologyFilter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="destination">The destination value.</param>
    /// <param name="topologyFilter">The topology filter value.</param>
    protected SqsMoveTransport(string destination, ConfigureAmazonSqsTopologyFilter<TSettings> topologyFilter)
    {
        _destination = destination;
        _topologyFilter = topologyFilter;

        _isFifo = AmazonSqsEndpointAddress.IsFifo(destination);
    }

    /// <summary>
    /// Performs the move operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="preSend">The pre send value.</param>
    /// <returns>The result of the operation.</returns>
    protected async Task MoveAsync(ReceiveContext context, Action<SendMessageBatchRequestEntry, IDictionary<string, MessageAttributeValue>> preSend)
    {
        if (!context.TryGetPayload(out ClientContext? clientContext))
            throw new ArgumentException("The ReceiveContext must contain a ClientContext (from Amazon SQS)", nameof(context));

        OneTimeContext<ConfigureTopologyContext<TSettings>> oneTimeContext =
            await _topologyFilter.ConfigureAsync(clientContext, context.CancellationToken).ConfigureAwait(false);

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
            await clientContext.SendMessageAsync(_destination, message, context.CancellationToken).ConfigureAwait(false);
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
