using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace ViciOne.ServiceBus.Logging;

/// <summary>Provides extension methods for saga log.</summary>
public static class SagaLogExtensions
{
    static readonly LogMessage<string, Guid?, string> _logUsed = LogContext.Define<string, Guid?, string>(LogLevel.Debug,
        "SAGA:{SagaType}:{CorrelationId} Used {MessageType}");

    static readonly LogMessage<string, Guid?, string> _logAdded = LogContext.Define<string, Guid?, string>(LogLevel.Debug,
        "SAGA:{SagaType}:{CorrelationId} Added {MessageType}");

    static readonly LogMessage<string, Guid?, string> _logCreated = LogContext.Define<string, Guid?, string>(LogLevel.Debug,
        "SAGA:{SagaType}:{CorrelationId} Created {MessageType}");

    static readonly LogMessage<string, Guid?, string> _logInserted = LogContext.Define<string, Guid?, string>(LogLevel.Debug,
        "SAGA:{SagaType}:{CorrelationId} Used {MessageType}");

    static readonly LogMessage<string, Guid?, string> _logInsertFaulted = LogContext.Define<string, Guid?, string>(LogLevel.Debug,
        "SAGA:{SagaType}:{CorrelationId} Dupe {MessageType}");

    static readonly LogMessage<string, Guid?, string> _logRemoved = LogContext.Define<string, Guid?, string>(LogLevel.Debug,
        "SAGA:{SagaType}:{CorrelationId} Removed {MessageType}");

    static readonly LogMessage<string, Guid?, string> _logFaulted = LogContext.Define<string, Guid?, string>(LogLevel.Error,
        "SAGA:{SagaType}:{CorrelationId} Fault {MessageType}");

    /// <summary>Logs used.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="correlationId">The correlation id.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogUsed<TSaga, TMessage>(this SagaConsumeContext<TSaga, TMessage> context, Guid? correlationId = default)
        where TSaga : class, ISaga
        where TMessage : class
    {
        _logUsed(TypeCache<TSaga>.ShortName, context.CorrelationId ?? correlationId, TypeCache<TMessage>.ShortName);
    }

    /// <summary>Logs added.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="correlationId">The correlation id.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogAdded<TSaga, TMessage>(this SagaConsumeContext<TSaga, TMessage> context, Guid? correlationId = default)
        where TSaga : class, ISaga
        where TMessage : class
    {
        _logAdded(TypeCache<TSaga>.ShortName, context.CorrelationId ?? correlationId, TypeCache<TMessage>.ShortName);
    }

    /// <summary>Reports that log has been created.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="correlationId">The correlation id.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogCreated<TSaga, TMessage>(this SagaConsumeContext<TSaga, TMessage> context, Guid? correlationId = default)
        where TSaga : class, ISaga
        where TMessage : class
    {
        _logCreated(TypeCache<TSaga>.ShortName, context.CorrelationId ?? correlationId, TypeCache<TMessage>.ShortName);
    }

    /// <summary>Logs insert.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="correlationId">The correlation id.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogInsert<TSaga, TMessage>(this ConsumeContext<TMessage> context, Guid? correlationId = default)
        where TSaga : class, ISaga
        where TMessage : class
    {
        _logInserted(TypeCache<TSaga>.ShortName, context.CorrelationId ?? correlationId, TypeCache<TMessage>.ShortName);
    }

    /// <summary>Logs insert fault.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="correlationId">The correlation id.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogInsertFault<TSaga, TMessage>(this ConsumeContext<TMessage> context, Exception exception,
        Guid? correlationId = default)
        where TSaga : class, ISaga
        where TMessage : class
    {
        _logInsertFaulted(TypeCache<TSaga>.ShortName, context.CorrelationId ?? correlationId, TypeCache<TMessage>.ShortName, exception);
    }

    /// <summary>Logs removed.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="correlationId">The correlation id.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogRemoved<TSaga, TMessage>(this SagaConsumeContext<TSaga, TMessage> context, Guid? correlationId = default)
        where TSaga : class, ISaga
        where TMessage : class
    {
        _logRemoved(TypeCache<TSaga>.ShortName, context.CorrelationId ?? correlationId, TypeCache<TMessage>.ShortName);
    }

    /// <summary>Logs fault.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="correlationId">The correlation id.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogFault<TSaga, TMessage>(this ConsumeContext<TMessage> context, Exception exception, Guid? correlationId = default)
        where TSaga : class, ISaga
        where TMessage : class
    {
        _logFaulted(TypeCache<TSaga>.ShortName, context.CorrelationId ?? correlationId, TypeCache<TMessage>.ShortName, exception);
    }
}
