using System.Globalization;
using System.Text;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Reads standard service-bus metadata from receive and header contexts.</summary>
public static class ReceiveContextExtensions
{
    /// <summary>Gets the message identifier from the transport headers.</summary>
    /// <param name="context">The receive context.</param>
    /// <returns>The message identifier, or <see langword="null" /> when unavailable or invalid.</returns>
    public static Guid? GetMessageId(this ReceiveContext context)
    {
        return GetTransportHeaders(context).GetMessageId();
    }

    /// <summary>Gets the message identifier from the transport headers.</summary>
    /// <param name="context">The receive context.</param>
    /// <param name="defaultValue">The value returned when the header is unavailable or invalid.</param>
    /// <returns>The message identifier or the supplied fallback.</returns>
    public static Guid GetMessageId(this ReceiveContext context, Guid defaultValue)
    {
        return GetTransportHeaders(context).GetMessageId(defaultValue);
    }

    /// <summary>Gets the correlation identifier from the transport headers.</summary>
    /// <param name="context">The receive context.</param>
    /// <returns>The correlation identifier, or <see langword="null" /> when unavailable or invalid.</returns>
    public static Guid? GetCorrelationId(this ReceiveContext context)
    {
        return GetTransportHeaders(context).GetCorrelationId();
    }

    /// <summary>Gets the conversation identifier from the transport headers.</summary>
    /// <param name="context">The receive context.</param>
    /// <returns>The conversation identifier, or <see langword="null" /> when unavailable or invalid.</returns>
    public static Guid? GetConversationId(this ReceiveContext context)
    {
        return GetTransportHeaders(context).GetConversationId();
    }

    /// <summary>Gets the request identifier from the transport headers.</summary>
    /// <param name="context">The receive context.</param>
    /// <returns>The request identifier, or <see langword="null" /> when unavailable or invalid.</returns>
    public static Guid? GetRequestId(this ReceiveContext context)
    {
        return GetTransportHeaders(context).GetRequestId();
    }

    /// <summary>Gets the initiator identifier from the transport headers.</summary>
    /// <param name="context">The receive context.</param>
    /// <returns>The initiator identifier, or <see langword="null" /> when unavailable or invalid.</returns>
    public static Guid? GetInitiatorId(this ReceiveContext context)
    {
        return GetTransportHeaders(context).GetInitiatorId();
    }

    /// <summary>Gets the absolute source address from the transport headers.</summary>
    /// <param name="context">The receive context.</param>
    /// <returns>The source address, or <see langword="null" /> when unavailable or invalid.</returns>
    public static Uri? GetSourceAddress(this ReceiveContext context)
    {
        return GetTransportHeaders(context).GetSourceAddress();
    }

    /// <summary>Gets the absolute response address from the transport headers.</summary>
    /// <param name="context">The receive context.</param>
    /// <returns>The response address, or <see langword="null" /> when unavailable or invalid.</returns>
    public static Uri? GetResponseAddress(this ReceiveContext context)
    {
        return GetTransportHeaders(context).GetResponseAddress();
    }

    /// <summary>Gets the absolute fault address from the transport headers.</summary>
    /// <param name="context">The receive context.</param>
    /// <returns>The fault address, or <see langword="null" /> when unavailable or invalid.</returns>
    public static Uri? GetFaultAddress(this ReceiveContext context)
    {
        return GetTransportHeaders(context).GetFaultAddress();
    }

    /// <summary>Gets the transport-assigned message sent time.</summary>
    /// <param name="context">The receive context.</param>
    /// <returns>The sent time, or <see langword="null" /> when unavailable or invalid.</returns>
    public static DateTimeOffset? GetSentTime(this ReceiveContext context)
    {
        return GetTimestamp(GetTransportHeaders(context), MessageHeaders.TransportSentTime);
    }

    /// <summary>Gets the message-type identifiers declared by the transport headers.</summary>
    /// <param name="context">The receive context.</param>
    /// <returns>The normalized message-type identifiers.</returns>
    public static string[] GetMessageTypes(this ReceiveContext context)
    {
        return GetTransportHeaders(context).GetMessageTypes();
    }

    /// <summary>Gets the declared content encoding or the default UTF-8 encoding when none is declared.</summary>
    /// <param name="context">The receive context.</param>
    /// <returns>The message body encoding.</returns>
    public static Encoding GetMessageEncoding(this ReceiveContext context)
    {
        return GetTransportHeaders(context).GetMessageEncoding();
    }

    /// <summary>Gets the message identifier from a header collection.</summary>
    /// <param name="headers">The header collection.</param>
    /// <returns>The message identifier, or <see langword="null" /> when unavailable or invalid.</returns>
    public static Guid? GetMessageId(this Headers headers)
    {
        return headers.GetHeaderId(MessageHeaders.MessageId);
    }

    /// <summary>Gets the message identifier from a header collection.</summary>
    /// <param name="headers">The header collection.</param>
    /// <param name="defaultValue">The value returned when the header is unavailable or invalid.</param>
    /// <returns>The message identifier or the supplied fallback.</returns>
    public static Guid GetMessageId(this Headers headers, Guid defaultValue)
    {
        return headers.GetHeaderId(MessageHeaders.MessageId, defaultValue);
    }

    /// <summary>Gets the correlation identifier from a header collection.</summary>
    /// <param name="headers">The header collection.</param>
    /// <returns>The correlation identifier, or <see langword="null" /> when unavailable or invalid.</returns>
    public static Guid? GetCorrelationId(this Headers headers)
    {
        return headers.GetHeaderId(MessageHeaders.CorrelationId);
    }

    /// <summary>Gets the conversation identifier from a header collection.</summary>
    /// <param name="headers">The header collection.</param>
    /// <returns>The conversation identifier, or <see langword="null" /> when unavailable or invalid.</returns>
    public static Guid? GetConversationId(this Headers headers)
    {
        return headers.GetHeaderId(MessageHeaders.ConversationId);
    }

    /// <summary>Gets the request identifier from a header collection.</summary>
    /// <param name="headers">The header collection.</param>
    /// <returns>The request identifier, or <see langword="null" /> when unavailable or invalid.</returns>
    public static Guid? GetRequestId(this Headers headers)
    {
        return headers.GetHeaderId(MessageHeaders.RequestId);
    }

    /// <summary>Gets the initiator identifier from a header collection.</summary>
    /// <param name="headers">The header collection.</param>
    /// <returns>The initiator identifier, or <see langword="null" /> when unavailable or invalid.</returns>
    public static Guid? GetInitiatorId(this Headers headers)
    {
        return headers.GetHeaderId(MessageHeaders.InitiatorId);
    }

    /// <summary>Gets the absolute source address from a header collection.</summary>
    /// <param name="headers">The header collection.</param>
    /// <returns>The source address, or <see langword="null" /> when unavailable or invalid.</returns>
    public static Uri? GetSourceAddress(this Headers headers)
    {
        return headers.GetEndpointAddress(MessageHeaders.SourceAddress);
    }

    /// <summary>Gets the absolute response address from a header collection.</summary>
    /// <param name="headers">The header collection.</param>
    /// <returns>The response address, or <see langword="null" /> when unavailable or invalid.</returns>
    public static Uri? GetResponseAddress(this Headers headers)
    {
        return headers.GetEndpointAddress(MessageHeaders.ResponseAddress);
    }

    /// <summary>Gets the absolute fault address from a header collection.</summary>
    /// <param name="headers">The header collection.</param>
    /// <returns>The fault address, or <see langword="null" /> when unavailable or invalid.</returns>
    public static Uri? GetFaultAddress(this Headers headers)
    {
        return headers.GetEndpointAddress(MessageHeaders.FaultAddress);
    }

    /// <summary>Gets normalized message-type identifiers from a header collection.</summary>
    /// <param name="headers">The header collection.</param>
    /// <returns>The declared message-type identifiers without empty entries.</returns>
    public static string[] GetMessageTypes(this Headers headers)
    {
        ArgumentNullException.ThrowIfNull(headers);
        if (headers.TryGetHeader(MessageHeaders.MessageType, out object? value)
            && value is string text
            && !string.IsNullOrWhiteSpace(text))
            return text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return [];
    }

    /// <summary>Gets the declared content encoding or the default UTF-8 encoding when none is declared.</summary>
    /// <param name="headers">The header collection.</param>
    /// <returns>The message body encoding.</returns>
    public static Encoding GetMessageEncoding(this Headers headers)
    {
        ArgumentNullException.ThrowIfNull(headers);
        if (headers.TryGetHeader("Content-Encoding", out object? value)
            && value is string text
            && !string.IsNullOrWhiteSpace(text))
            return Encoding.GetEncoding(text);

        return MessageDefaults.Encoding;
    }

    /// <summary>Gets an identifier from a named header.</summary>
    /// <param name="headers">The header collection.</param>
    /// <param name="key">The header name.</param>
    /// <param name="defaultValue">The value returned when the header is unavailable or invalid.</param>
    /// <returns>The parsed identifier or the supplied fallback.</returns>
    public static Guid GetHeaderId(this Headers headers, string key, Guid defaultValue)
    {
        return GetHeaderId(headers, key) ?? defaultValue;
    }

    /// <summary>Gets an identifier from a named header.</summary>
    /// <param name="headers">The header collection.</param>
    /// <param name="key">The header name.</param>
    /// <returns>The parsed identifier, or <see langword="null" /> when unavailable or invalid.</returns>
    public static Guid? GetHeaderId(this Headers headers, string key)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (headers.TryGetHeader(key, out object? value))
        {
            return value switch
            {
                Guid guid => guid,
                string text when Guid.TryParse(text, out Guid guid) => guid,
                _ => null,
            };
        }

        return null;
    }

    private static DateTimeOffset? GetTimestamp(Headers headers, string key)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (headers.TryGetHeader(key, out object? value))
        {
            return value switch
            {
                DateTimeOffset timestamp => timestamp.ToUniversalTime(),
                DateTime timestamp => new DateTimeOffset(
                    timestamp.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(timestamp, DateTimeKind.Utc)
                        : timestamp.ToUniversalTime()),
                string text when DateTimeOffset.TryParse(
                    text,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out DateTimeOffset timestamp) => timestamp,
                _ => null,
            };
        }

        return null;
    }

    /// <summary>Gets an absolute endpoint address from a named header.</summary>
    /// <param name="headers">The header collection.</param>
    /// <param name="key">The header name.</param>
    /// <returns>The absolute address, or <see langword="null" /> when unavailable, relative, or invalid.</returns>
    public static Uri? GetEndpointAddress(this Headers headers, string key)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (headers.TryGetHeader(key, out object? value))
        {
            return value switch
            {
                Uri { IsAbsoluteUri: true } address => address,
                string text when Uri.TryCreate(text, UriKind.Absolute, out Uri? address) => address,
                _ => null,
            };
        }

        return null;
    }

    private static Headers GetTransportHeaders(ReceiveContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.TransportHeaders;
    }
}
