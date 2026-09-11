using System.Globalization;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Applies standard host, fault, causality, and redelivery metadata to outgoing contexts.</summary>
public static class SendContextExtensions
{
    /// <summary>Writes the current process and runtime identity to an outgoing header collection.</summary>
    /// <param name="headers">The destination header collection.</param>
    public static void SetHostHeaders(this SendHeaders headers)
    {
        ArgumentNullException.ThrowIfNull(headers);
        headers.Set(MessageHeaders.Host.MachineName, HostMetadataCache.Host.MachineName);
        headers.Set(MessageHeaders.Host.ProcessName, HostMetadataCache.Host.ProcessName);
        headers.Set(MessageHeaders.Host.ProcessId, HostMetadataCache.Host.ProcessId.ToString(CultureInfo.InvariantCulture));
        headers.Set(MessageHeaders.Host.Assembly, HostMetadataCache.Host.Assembly);
        headers.Set(MessageHeaders.Host.AssemblyVersion, HostMetadataCache.Host.AssemblyVersion);
        headers.Set(MessageHeaders.Host.ViciOneServiceBusVersion, HostMetadataCache.Host.ViciOneServiceBusVersion);
        headers.Set(MessageHeaders.Host.FrameworkVersion, HostMetadataCache.Host.FrameworkVersion);
        headers.Set(MessageHeaders.Host.OperatingSystemVersion, HostMetadataCache.Host.OperatingSystemVersion);
    }

    /// <summary>Writes the current process and runtime identity to a transport-specific header dictionary.</summary>
    /// <typeparam name="THeaderValue">The transport-specific header value type.</typeparam>
    /// <param name="adapter">The adapter that converts and writes header values.</param>
    /// <param name="dictionary">The destination header dictionary.</param>
    public static void SetHostHeaders<THeaderValue>(this ITransportSetHeaderAdapter<THeaderValue> adapter,
        IDictionary<string, THeaderValue> dictionary)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentNullException.ThrowIfNull(dictionary);
        adapter.Set(dictionary, MessageHeaders.Host.MachineName, HostMetadataCache.Host.MachineName);
        adapter.Set(dictionary, MessageHeaders.Host.ProcessName, HostMetadataCache.Host.ProcessName);
        adapter.Set(dictionary, MessageHeaders.Host.ProcessId, HostMetadataCache.Host.ProcessId);
        adapter.Set(dictionary, MessageHeaders.Host.Assembly, HostMetadataCache.Host.Assembly);
        adapter.Set(dictionary, MessageHeaders.Host.AssemblyVersion, HostMetadataCache.Host.AssemblyVersion);
        adapter.Set(dictionary, MessageHeaders.Host.ViciOneServiceBusVersion, HostMetadataCache.Host.ViciOneServiceBusVersion);
        adapter.Set(dictionary, MessageHeaders.Host.FrameworkVersion, HostMetadataCache.Host.FrameworkVersion);
        adapter.Set(dictionary, MessageHeaders.Host.OperatingSystemVersion, HostMetadataCache.Host.OperatingSystemVersion);
    }

    /// <summary>Writes standard fault metadata to an outgoing header collection.</summary>
    /// <param name="headers">The destination header collection.</param>
    /// <param name="exceptionContext">The receive failure to describe.</param>
    public static void SetExceptionHeaders(this SendHeaders headers, ExceptionReceiveContext exceptionContext)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(exceptionContext);
        Exception exception = exceptionContext.Exception.GetBaseException();

        string exceptionMessage = ExceptionUtil.GetMessage(exception);

        headers.Set(MessageHeaders.Reason, "fault");

        headers.Set(MessageHeaders.FaultExceptionType, TypeCache.GetShortName(exception.GetType()));
        headers.Set(MessageHeaders.FaultInputAddress, exceptionContext.InputAddress?.ToString());
        headers.Set(MessageHeaders.FaultMessage, exceptionMessage);
        headers.Set(MessageHeaders.FaultTimestamp, exceptionContext.ExceptionTimestamp.ToString("O", CultureInfo.InvariantCulture));
        headers.Set(MessageHeaders.FaultStackTrace, ExceptionUtil.GetStackTrace(exception));

        if (exceptionContext.TryGetPayload(out ConsumerFaultContext? info))
        {
            headers.Set(MessageHeaders.FaultConsumerType, info.ConsumerType);
            headers.Set(MessageHeaders.FaultMessageType, info.MessageType);
        }

        if (exceptionContext.TryGetPayload(out RetryContext? retryContext) && retryContext.RetryCount > 0)
            headers.Set(MessageHeaders.FaultRetryCount, retryContext.RetryCount);
    }

    /// <summary>Writes standard fault metadata to a transport-specific header dictionary.</summary>
    /// <typeparam name="THeaderValue">The transport-specific header value type.</typeparam>
    /// <param name="adapter">The adapter that converts and writes header values.</param>
    /// <param name="headers">The destination header dictionary.</param>
    /// <param name="exceptionContext">The receive failure to describe.</param>
    public static void SetExceptionHeaders<THeaderValue>(this ITransportSetHeaderAdapter<THeaderValue> adapter,
        IDictionary<string, THeaderValue> headers, ExceptionReceiveContext exceptionContext)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(exceptionContext);
        Exception exception = exceptionContext.Exception.GetBaseException();

        string exceptionMessage = ExceptionUtil.GetMessage(exception);

        adapter.Set(headers, MessageHeaders.Reason, "fault");

        adapter.Set(headers, MessageHeaders.FaultExceptionType, TypeCache.GetShortName(exception.GetType()));
        adapter.Set(headers, MessageHeaders.FaultInputAddress, exceptionContext.InputAddress);
        adapter.Set(headers, MessageHeaders.FaultMessage, exceptionMessage);
        adapter.Set(headers, MessageHeaders.FaultTimestamp, exceptionContext.ExceptionTimestamp);
        adapter.Set(headers, MessageHeaders.FaultStackTrace, ExceptionUtil.GetStackTrace(exception));

        if (exceptionContext.TryGetPayload(out ConsumerFaultContext? info))
        {
            adapter.Set(headers, MessageHeaders.FaultConsumerType, info.ConsumerType);
            adapter.Set(headers, MessageHeaders.FaultMessageType, info.MessageType);
        }

        if (exceptionContext.TryGetPayload(out RetryContext? retryContext) && retryContext.RetryCount > 0)
            adapter.Set(headers, MessageHeaders.FaultRetryCount, retryContext.RetryCount);
    }

    /// <summary>Transfers the active consume scope, message causality, and application headers to an outgoing context.</summary>
    /// <param name="sendContext">The destination send context.</param>
    /// <param name="consumeContext">The source consume context.</param>
    public static void TransferConsumeContextHeaders(this SendContext sendContext, ConsumeContext consumeContext)
    {
        ArgumentNullException.ThrowIfNull(sendContext);
        ArgumentNullException.ThrowIfNull(consumeContext);
        sendContext.AddOrUpdatePayload(() => consumeContext, _ => consumeContext);

        sendContext.SourceAddress = consumeContext.ReceiveContext.InputAddress;

        if (consumeContext.ConversationId.HasValue)
            sendContext.ConversationId = consumeContext.ConversationId;

        if (consumeContext.CorrelationId.HasValue)
            sendContext.InitiatorId = consumeContext.CorrelationId;
        else if (consumeContext.RequestId.HasValue)
            sendContext.InitiatorId = consumeContext.RequestId;

        SendHeaders sendHeaders = sendContext.Headers;

        foreach (KeyValuePair<string, object> header in consumeContext.Headers.GetAll())
        {
            if (header.Key.StartsWith(MessageHeaders.Prefix, StringComparison.Ordinal))
                continue;

            sendHeaders.Set(header.Key, header.Value, false);
        }
    }

    /// <summary>Applies redelivery identity and attempt metadata to an outgoing context.</summary>
    /// <param name="sendContext">The outgoing redelivery context.</param>
    /// <param name="consumeContext">The consumed message being redelivered.</param>
    /// <param name="options">The redelivery options.</param>
    public static void ApplyRedeliveryOptions(this SendContext sendContext, ConsumeContext consumeContext, RedeliveryOptions options)
    {
        ArgumentNullException.ThrowIfNull(sendContext);
        ArgumentNullException.ThrowIfNull(consumeContext);
        if (options.HasFlag(RedeliveryOptions.ReplaceMessageId))
            sendContext.ReplaceMessageId(consumeContext);

        sendContext.Headers.Set(MessageHeaders.RedeliveryCount, consumeContext.GetRedeliveryCount() + 1);
    }

    /// <summary>Assigns a new message identifier while preserving the earliest known identifier as a header.</summary>
    /// <param name="sendContext">The outgoing context whose identifier is replaced.</param>
    /// <param name="consumeContext">The source context used to preserve message lineage.</param>
    /// <returns>The supplied send context.</returns>
    public static SendContext ReplaceMessageId(this SendContext sendContext, ConsumeContext consumeContext)
    {
        ArgumentNullException.ThrowIfNull(sendContext);
        ArgumentNullException.ThrowIfNull(consumeContext);
        if (consumeContext.TryGetHeader(MessageHeaders.OriginalMessageId, out Guid? originalMessageId) && originalMessageId.HasValue)
            sendContext.Headers.Set(MessageHeaders.OriginalMessageId, originalMessageId.ToString());
        else if (sendContext.MessageId.HasValue)
            sendContext.Headers.Set(MessageHeaders.OriginalMessageId, sendContext.MessageId.ToString());

        sendContext.MessageId = NewId.NextGuid();

        return sendContext;
    }

    /// <summary>Gets the earliest known message identifier from the lineage header or current message metadata.</summary>
    /// <param name="context">The consume context.</param>
    /// <returns>The original or current message identifier.</returns>
    public static Guid? GetOriginalMessageId(this ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.TryGetHeader(MessageHeaders.OriginalMessageId, out Guid? originalMessageId)
            ? originalMessageId
            : context.MessageId;
    }

    /// <summary>
    /// Starts a new conversation and preserves the current conversation identifier as initiating metadata.
    /// </summary>
    /// <param name="context">The send context.</param>
    /// <returns>The supplied send context.</returns>
    public static SendContext StartNewConversation(this SendContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return StartNewConversation(context, NewId.NextGuid());
    }

    /// <summary>
    /// Starts a new conversation with an explicit identifier and preserves the current identifier as initiating metadata.
    /// </summary>
    /// <param name="context">The send context.</param>
    /// <param name="conversationId">The new conversation identifier.</param>
    /// <returns>The supplied send context.</returns>
    public static SendContext StartNewConversation(this SendContext context, Guid conversationId)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (conversationId == Guid.Empty)
            throw new ArgumentException("The conversation identifier cannot be empty.", nameof(conversationId));

        if (context.ConversationId.HasValue)
            context.Headers.Set(MessageHeaders.InitiatingConversationId, context.ConversationId.Value.ToString());

        context.ConversationId = conversationId;

        return context;
    }
}
