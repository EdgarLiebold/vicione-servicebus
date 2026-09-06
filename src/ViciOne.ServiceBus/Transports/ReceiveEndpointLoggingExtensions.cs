using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Provides extension methods for receive endpoint logging.</summary>
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

    static readonly LogMessage<Uri, string?, object> _logReceiveDupe = LogContext.Define<Uri, string?, object>(LogLevel.Warning,
        "R-DUPE {InputAddress} {MessageId} {TransportMessageId}");

    static readonly LogMessage<Uri, Guid?, string> _logSent = LogContext.DefineMessage<Uri, Guid?, string>(LogLevel.Debug,
        "SEND {DestinationAddress} {MessageId} {MessageType}");

    static readonly LogMessage<Uri, Guid?, string> _logSendFault = LogContext.Define<Uri, Guid?, string>(LogLevel.Error,
        "S-FAULT {DestinationAddress} {MessageId} {MessageType}");

    static readonly LogMessage<Uri, Guid?, string, DateTimeOffset?, TimeSpan?> _logExpiredForward =
        LogContext.DefineMessage<Uri, Guid?, string, DateTimeOffset?, TimeSpan?>(LogLevel.Information,
            "FORWARD-EXPIRED {DestinationAddress} {MessageId} {MessageType} {ExpirationTime} {TimeToLive}");

    static readonly LogMessage<Uri, string?> _logSkipped = LogContext.DefineMessage<Uri, string?>(LogLevel.Debug,
        "SKIP {InputAddress} {MessageId}");

    static readonly LogMessage<Uri, Guid?, string> _logRetry = LogContext.Define<Uri, Guid?, string>(LogLevel.Warning,
        "R-RETRY {InputAddress} {MessageId} {MessageType}");

    static readonly LogMessage<Uri, string?> _logFault = LogContext.Define<Uri, string?>(LogLevel.Error,
        "T-FAULT {InputAddress} {MessageId}");

    static readonly LogMessage<Uri, Guid?, string, DateTimeOffset, Guid?> _logScheduled =
        LogContext.DefineMessage<Uri, Guid?, string, DateTimeOffset, Guid?>(
        LogLevel.Debug, "SCHED {DestinationAddress} {MessageId} {MessageType} {DeliveryTime:G} {Token}");

    static readonly LogMessage<Uri, long, int> _logConsumerCompleted = LogContext.DefineMessage<Uri, long, int>(
        LogLevel.Debug, "Consumer Completed: {InputAddress}: {DeliveryCount} received, {ConcurrentDeliveryCount} concurrent");

    static readonly LogMessage<Uri, long, int, string> _logConsumerCompletedTag = LogContext.DefineMessage<Uri, long, int, string>(
        LogLevel.Debug, "Consumer Completed: {InputAddress}: {DeliveryCount} received, {ConcurrentDeliveryCount} concurrent, {Tag}");

    /// <summary>Log a skipped message that was moved to the dead-letter queue.</summary>
    /// <param name="context">The context associated with the operation.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogSkipped(this ReceiveContext context)
    {
        _logSkipped(context.InputAddress, GetMessageId(context));
    }

    /// <summary>Log a moved message from one endpoint to the destination endpoint address.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="reason">The reason.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogMoved(this ReceiveContext context, string destination, string reason)
    {
        _logMoved(context.InputAddress, GetMessageId(context), destination, reason);
    }

    /// <summary>Log a consumed message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogConsumed<T>(this ConsumeContext<T> context, TimeSpan duration, string consumerType)
        where T : class
    {
        _logConsumed(context.Advanced().ReceiveContext.InputAddress, context.MessageId, TypeCache<T>.ShortName, consumerType, duration);
    }

    /// <summary>Reports that log has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogFaulted<T>(this ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
        where T : class
    {
        _logConsumeFault(context.Advanced().ReceiveContext.InputAddress, context.MessageId, TypeCache<T>.ShortName, consumerType, duration, exception);
    }

    /// <summary>Logs canceled.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogCanceled<T>(this ConsumeContext<T> context, TimeSpan duration, string consumerType)
        where T : class
    {
        _logConsumeCanceled(context.Advanced().ReceiveContext.InputAddress, context.MessageId, TypeCache<T>.ShortName, consumerType, duration);
    }

    /// <summary>Reports that log has faulted.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogFaulted(this ReceiveContext context, Exception exception)
    {
        _logReceiveFault(context.InputAddress, GetMessageId(context), context.ElapsedTime, exception);
    }

    /// <summary>Logs transport dupe.</summary>
    /// <typeparam name="TTransportMessageId">The ransport message id type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="transportMessageId">The transport message id.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogTransportDupe<TTransportMessageId>(this ReceiveContext context, TTransportMessageId transportMessageId)
    {
        _logReceiveDupe(context.InputAddress, GetMessageId(context), transportMessageId);
    }

    /// <summary>Reports that log transport has faulted.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogTransportFaulted(this ReceiveContext context, Exception exception)
    {
        if (exception.GetBaseException() is OperationCanceledException && context.CancellationToken.IsCancellationRequested)
            return;

        _logFault(context.InputAddress, GetMessageId(context), exception);
    }

    /// <summary>Logs retry.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogRetry(this ConsumeContext context, Exception exception)
    {
        _logRetry(context.Advanced().ReceiveContext.InputAddress, context.MessageId, TypeCache.GetShortName(context.GetType()), exception);
    }

    /// <summary>Logs retry.</summary>
    /// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogRetry<TContext>(this TContext context, Exception exception)
        where TContext : class, ConsumeContext
    {
        _logRetry(context.Advanced().ReceiveContext.InputAddress, context.MessageId, TypeCache<TContext>.ShortName, exception);
    }

    /// <summary>Reports that log has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogFaulted<T>(this SendContext<T> context, Exception exception)
        where T : class
    {
        _logSendFault(context.DestinationAddress, context.MessageId, TypeCache<T>.ShortName, exception);
    }

    /// <summary>Logs sent.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogSent<T>(this SendContext<T> context)
        where T : class
    {
        _logSent(context.DestinationAddress, context.MessageId, TypeCache<T>.ShortName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void LogExpiredForward<T>(this SendContext<T> context, DateTimeOffset? expirationTime, TimeSpan? timeToLive)
        where T : class
    {
        _logExpiredForward(context.DestinationAddress, context.MessageId, TypeCache<T>.ShortName, expirationTime, timeToLive);
    }

    /// <summary>Reports that log consumer has completed.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="deliveryCount">The delivery count.</param>
    /// <param name="concurrentDeliveryCount">The concurrent delivery count.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogConsumerCompleted(this ReceiveEndpointContext context, long deliveryCount, int concurrentDeliveryCount)
    {
        _logConsumerCompleted(context.InputAddress, deliveryCount, concurrentDeliveryCount);
    }

    /// <summary>Reports that log consumer has completed.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="deliveryCount">The delivery count.</param>
    /// <param name="concurrentDeliveryCount">The concurrent delivery count.</param>
    /// <param name="tag">The tag.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogConsumerCompleted(this ReceiveEndpointContext context, long deliveryCount, int concurrentDeliveryCount, string tag)
    {
        _logConsumerCompletedTag(context.InputAddress, deliveryCount, concurrentDeliveryCount, tag);
    }

    /// <summary>Logs scheduled.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="deliveryTime">The delivery time.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogScheduled<T>(this SendContext<T> context, DateTimeOffset deliveryTime)
        where T : class
    {
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
            return default;
        }
    }
}
