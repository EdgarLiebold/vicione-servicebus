using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Reads service-bus headers from Amazon SQS message attributes and system attributes.</summary>
public class AmazonSqsHeaderProvider :
    IHeaderProvider
{
    readonly SqsMessageBody _body;
    readonly Message _message;

    /// <summary>Creates a header provider for a received Amazon SQS message.</summary>
    /// <param name="message">The native queue message.</param>
    /// <param name="body">The parsed message body, including an optional Amazon SNS topic ARN.</param>
    public AmazonSqsHeaderProvider(Message message, SqsMessageBody body)
    {
        _message = message;
        _body = body;
    }

    /// <summary>Tries to read a message attribute, message identifier, topic ARN, or sent timestamp.</summary>
    /// <param name="key">The header name.</param>
    /// <param name="value">The header value, when available.</param>
    /// <returns><see langword="true" /> when the header is available; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
    {
        if (_message.MessageAttributes != null && _message.MessageAttributes.TryGetValue(key, out var val))
        {
            value = val.StringValue;
            return value != null;
        }

        if (nameof(Message.MessageId).Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            value = _message.MessageId;
            return true;
        }

        if ("TopicArn".Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            value = _body.TopicArn;
            return value != null;
        }

        if (MessageHeaders.TransportSentTime.Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            if (_message.Attributes != null && _message.Attributes.TryGetValue(MessageSystemAttributeName.SentTimestamp, out var sentTimestamp))
            {
                if (long.TryParse(sentTimestamp, out var milliseconds))
                {
                    // SQS defines SentTimestamp as Unix epoch milliseconds, which is an absolute UTC instant.
                    // DateTimeConstants.Epoch is intentionally kind-agnostic for general conversions, so using it
                    // here would incorrectly expose a provider timestamp as DateTimeKind.Unspecified.
                    value = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
                    return true;
                }
            }
        }

        value = null;
        return false;
    }

    /// <summary>Enumerates the logical message identifier and all non-null string message attributes.</summary>
    /// <returns>The available header name/value pairs.</returns>
    public IEnumerable<KeyValuePair<string, object>> GetAll()
    {
        if (!TryGetHeader(MessageHeaders.MessageId, out _))
            yield return new KeyValuePair<string, object>(MessageHeaders.MessageId, _message.MessageId);

        if (_message.MessageAttributes != null)
        {
            foreach (KeyValuePair<string, object> header in _message.MessageAttributes
                         .Where(x => x.Value.StringValue != null)
                         .Select(x => new KeyValuePair<string, object>(x.Key, x.Value.StringValue)))
                yield return header;
        }
    }
}
