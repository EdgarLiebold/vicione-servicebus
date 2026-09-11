using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Writes receive, consume, send, retry, forwarding and scheduling events through precompiled log templates.</summary>
public static class ReceiveEndpointLoggingExtensions
{
    static readonly LogMessage<Uri, Guid?, string, string, TimeSpan> _logConsumed = LogContext.DefineMessage<Uri, Guid?, string, string, TimeSpan>(
        LogLevel.Debug, "RECEIVE {InputAddress} {MessageId} {MessageType} {ConsumerType}({Duration})");

    static readonly LogMessage<Uri, Guid?, string, string, TimeSpan> _logConsumeFault = LogContext.Define<Uri, Guid?, string, string, TimeSpan>(
        LogLevel.Error, "R-FAULT {InputAddress} {MessageId} {MessageType} {ConsumerType}({Duration})");

    static readonly LogMessage<Uri, Guid?, string, string, TimeSpan> _logConsumeCanceled = LogContext.Define<Uri, Guid?, string, string, TimeSpan>(
        LogLevel.Information, "R-CANCEL {InputAddress} {MessageId} {MessageType} {ConsumerType}({Duration})");

    static readonly LogMessage<Uri, string?, string, string> _logMoved = LogContext.DefineMessage<Uri, string?, string, string>(LogLevel.Information,
        "MOVE {InputAddress} {MessageId} {DestinationAddress} {Reason}");

    static readonly LogMessage<Uri, string?, TimeSpan> _logReceiveFault = LogContext.Define<Uri, string?, TimeSpan>(LogLevel.Error,
        "R-FAULT {InputAddress} {MessageId} {Duration}");

    static readonly LogMessage<Uri, string?, object?> _logReceiveDupe = LogContext.Define<Uri, string?, object?>(LogLevel.Warning,
        "R-DUPE {InputAddress} {MessageId} {TransportMessageId}");

    static readonly LogMessage<Uri?, Guid?, string> _logSent = LogContext.DefineMessage<Uri?, Guid?, string>(LogLevel.Debug,
        "SEND {DestinationAddress} {MessageId} {MessageType}");

    static readonly LogMessage<Uri?, Guid?, string> _logSendFault = LogContext.Define<Uri?, Guid?, string>(LogLevel.Error,
        "S-FAULT {DestinationAddress} {MessageId} {MessageType}");

    static readonly LogMessage<Uri?, Guid?, string, DateTimeOffset?, TimeSpan?> _logExpiredForward =
        LogContext.DefineMessage<Uri?, Guid?, string, DateTimeOffset?, TimeSpan?>(LogLevel.Information,
            "FORWARD-EXPIRED {DestinationAddress} {MessageId} {MessageType} {ExpirationTime} {TimeToLive}");

    static readonly LogMessage<Uri, string?> _logSkipped = LogContext.DefineMessage<Uri, string?>(LogLevel.Debug,
        "SKIP {InputAddress} {MessageId}");

    static readonly LogMessage<Uri, Guid?, string> _logRetry = LogContext.Define<Uri, Guid?, string>(LogLevel.Warning,
        "R-RETRY {InputAddress} {MessageId} {MessageType}");

    static readonly LogMessage<Uri, string?> _logFault = LogContext.Define<Uri, string?>(LogLevel.Error,
        "T-FAULT {InputAddress} {MessageId}");

    static readonly LogMessage<Uri?, Guid?, string, DateTimeOffset, Guid?> _logScheduled =
        LogContext.DefineMessage<Uri?, Guid?, string, DateTimeOffset, Guid?>(
        LogLevel.Debug, "SCHED {DestinationAddress} {MessageId} {MessageType} {DeliveryTime:G} {Token}");

    static readonly LogMessage<Uri, long, int> _logConsumerCompleted = LogContext.DefineMessage<Uri, long, int>(
        LogLevel.Debug, "Consumer Completed: {InputAddress}: {DeliveryCount} received, {MaxConcurrentDeliveryCount} concurrent peak");

    static readonly LogMessage<Uri, long, int, string> _logConsumerCompletedTag = LogContext.DefineMessage<Uri, long, int, string>(
        LogLevel.Debug, "Consumer Completed: {InputAddress}: {DeliveryCount} received, {MaxConcurrentDeliveryCount} concurrent peak, {Tag}");

    /// <summary>Logs a skipped message after it has been moved to the dead-letter endpoint.</summary>
    /// <param name="context">The receive context for the skipped message.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogSkipped(this ReceiveContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _logSkipped(context.InputAddress, GetMessageId(context));
    }

    /// <summary>Logs a message moved from its input endpoint to another destination.</summary>
    /// <param name="context">The receive context for the moved message.</param>
    /// <param name="destination">The destination address.</param>
    /// <param name="reason">The reason for moving the message.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogMoved(this ReceiveContext context, string destination, string reason)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        _logMoved(context.InputAddress, GetMessageId(context), destination, reason);
    }

    /// <summary>Logs successful message consumption.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="duration">The elapsed consumption time.</param>
    /// <param name="consumerType">The consumer type name.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogConsumed<T>(this ConsumeContext<T> context, TimeSpan duration, string consumerType)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerType);
        _logConsumed(context.Advanced().ReceiveContext.InputAddress, context.MessageId, TypeCache<T>.ShortName, consumerType, duration);
    }

    /// <summary>Logs failed message consumption.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="duration">The elapsed consumption time.</param>
    /// <param name="consumerType">The consumer type name.</param>
    /// <param name="exception">The consumption failure.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogFaulted<T>(this ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerType);
        ArgumentNullException.ThrowIfNull(exception);
        _logConsumeFault(context.Advanced().ReceiveContext.InputAddress, context.MessageId, TypeCache<T>.ShortName, consumerType, duration, exception);
    }

    /// <summary>Logs canceled message consumption.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="duration">The elapsed consumption time.</param>
    /// <param name="consumerType">The consumer type name.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogCanceled<T>(this ConsumeContext<T> context, TimeSpan duration, string consumerType)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerType);
        _logConsumeCanceled(context.Advanced().ReceiveContext.InputAddress, context.MessageId, TypeCache<T>.ShortName, consumerType, duration);
    }

    /// <summary>Logs a receive-pipeline failure.</summary>
    /// <param name="context">The receive context.</param>
    /// <param name="exception">The receive failure.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogFaulted(this ReceiveContext context, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exception);
        _logReceiveFault(context.InputAddress, GetMessageId(context), context.ElapsedTime, exception);
    }

    /// <summary>Logs a duplicate transport message.</summary>
    /// <typeparam name="TTransportMessageId">The transport message identifier type.</typeparam>
    /// <param name="context">The receive context.</param>
    /// <param name="transportMessageId">The duplicate transport message identifier.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogTransportDupe<TTransportMessageId>(this ReceiveContext context, TTransportMessageId transportMessageId)
    {
        ArgumentNullException.ThrowIfNull(context);
        _logReceiveDupe(context.InputAddress, GetMessageId(context), transportMessageId);
    }

    /// <summary>Logs a transport failure unless shutdown cancellation caused it.</summary>
    /// <param name="context">The receive context.</param>
    /// <param name="exception">The transport failure.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogTransportFaulted(this ReceiveContext context, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exception);
        if (exception.GetBaseException() is OperationCanceledException && context.CancellationToken.IsCancellationRequested)
            return;

        _logFault(context.InputAddress, GetMessageId(context), exception);
    }

    /// <summary>Logs a message retry.</summary>
    /// <param name="context">The consume context being retried.</param>
    /// <param name="exception">The failure that caused the retry.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogRetry(this ConsumeContext context, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exception);
        _logRetry(context.Advanced().ReceiveContext.InputAddress, context.MessageId, TypeCache.GetShortName(context.GetType()), exception);
    }

    /// <summary>Logs a retry for a specialized consume context.</summary>
    /// <typeparam name="TContext">The consume-context type.</typeparam>
    /// <param name="context">The consume context being retried.</param>
    /// <param name="exception">The failure that caused the retry.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogRetry<TContext>(this TContext context, Exception exception)
        where TContext : class, ConsumeContext
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exception);
        _logRetry(context.Advanced().ReceiveContext.InputAddress, context.MessageId, TypeCache<TContext>.ShortName, exception);
    }

    /// <summary>Logs a failed send.</summary>
    /// <typeparam name="T">The sent message type.</typeparam>
    /// <param name="context">The send context.</param>
    /// <param name="exception">The send failure.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogFaulted<T>(this SendContext<T> context, Exception exception)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exception);
        _logSendFault(context.DestinationAddress, context.MessageId, TypeCache<T>.ShortName, exception);
    }

    /// <summary>Logs a successful send.</summary>
    /// <typeparam name="T">The sent message type.</typeparam>
    /// <param name="context">The send context.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogSent<T>(this SendContext<T> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        _logSent(context.DestinationAddress, context.MessageId, TypeCache<T>.ShortName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void LogExpiredForward<T>(this SendContext<T> context, DateTimeOffset? expirationTime, TimeSpan? timeToLive)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        _logExpiredForward(context.DestinationAddress, context.MessageId, TypeCache<T>.ShortName, expirationTime, timeToLive);
    }

    /// <summary>Logs endpoint consumer statistics after a consumer completes.</summary>
    /// <param name="context">The receive endpoint context.</param>
    /// <param name="deliveryCount">The number of delivered messages.</param>
    /// <param name="maxConcurrentDeliveryCount">The highest number of concurrent deliveries.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogConsumerCompleted(this ReceiveEndpointContext context, long deliveryCount, int maxConcurrentDeliveryCount)
    {
        ArgumentNullException.ThrowIfNull(context);
        _logConsumerCompleted(context.InputAddress, deliveryCount, maxConcurrentDeliveryCount);
    }

    /// <summary>Logs tagged endpoint consumer statistics after a consumer completes.</summary>
    /// <param name="context">The receive endpoint context.</param>
    /// <param name="deliveryCount">The number of delivered messages.</param>
    /// <param name="maxConcurrentDeliveryCount">The highest number of concurrent deliveries.</param>
    /// <param name="tag">The statistics tag.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogConsumerCompleted(this ReceiveEndpointContext context, long deliveryCount, int maxConcurrentDeliveryCount, string tag)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        _logConsumerCompletedTag(context.InputAddress, deliveryCount, maxConcurrentDeliveryCount, tag);
    }

    /// <summary>Logs a message accepted for scheduled delivery.</summary>
    /// <typeparam name="T">The scheduled message type.</typeparam>
    /// <param name="context">The send context.</param>
    /// <param name="deliveryTime">The scheduled delivery time.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogScheduled<T>(this SendContext<T> context, DateTimeOffset deliveryTime)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        _logScheduled(context.DestinationAddress, context.MessageId, TypeCache<T>.ShortName, deliveryTime, context.ScheduledMessageId);
    }

    static string? GetMessageId(ReceiveContext context)
    {
        try
        {
            return context.GetMessageId()?.ToString() ?? context.TransportHeaders.Get<string>(MessageHeaders.TransportMessageId);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
