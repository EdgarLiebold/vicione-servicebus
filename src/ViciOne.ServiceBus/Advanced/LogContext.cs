using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides the asynchronous-flow-local logging context used by bus operations.</summary>
public static class LogContext
{
    static readonly AsyncLocal<ILogContext?> _current;

    static LogContext()
    {
        _current = new AsyncLocal<ILogContext?>();
    }

    /// <summary>Gets the current critical-level logger.</summary>
    public static EnabledLogger? Critical => Current?.Critical;
    /// <summary>Gets the current debug-level logger.</summary>
    public static EnabledLogger? Debug => Current?.Debug;
    /// <summary>Gets the current error-level logger.</summary>
    public static EnabledLogger? Error => Current?.Error;
    /// <summary>Gets the current information-level logger.</summary>
    public static EnabledLogger? Info => Current?.Info;
    /// <summary>Gets the current trace-level logger.</summary>
    public static EnabledLogger? Trace => Current?.Trace;
    /// <summary>Gets the current warning-level logger.</summary>
    public static EnabledLogger? Warning => Current?.Warning;

    /// <summary>Gets or sets the logging context for the current asynchronous control flow.</summary>
    public static ILogContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    /// <summary>Configures the current context from a logger factory.</summary>
    /// <param name="loggerFactory">The logger factory, or <see langword="null"/> to disable output.</param>
    public static void ConfigureCurrentLogContext(ILoggerFactory? loggerFactory = null)
    {
        Current = new BusLogContext(loggerFactory ?? NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Configures the current context to route every category through one logger.
    /// </summary>
    /// <param name="logger">An existing logger.</param>
    public static void ConfigureCurrentLogContext(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        Current = new BusLogContext(new SingleLoggerFactory(logger));
    }

    /// <summary>Creates a child logging context for a category and preserves current instrumentation.</summary>
    /// <param name="categoryName">The non-empty logging category.</param>
    /// <returns>The category-specific logging context.</returns>
    public static ILogContext CreateLogContext(string categoryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);

        var current = Current ??= CreateDefaultLogContext();
        var created = current.CreateLogContext(categoryName);

        LogContextInstrumentationExtensions.CopyInstrumentation(current, created);
        return created;
    }

    /// <summary>
    /// Configures the current context from dependency injection when no usable logger is present,
    /// then attaches available logging instrumentation.
    /// </summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    public static void ConfigureCurrentLogContextIfNull(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        if (Current == null || Current.Logger is NullLogger)
        {
            var loggerFactory = provider.GetService<ILoggerFactory>();
            if (loggerFactory != null)
                ConfigureCurrentLogContext(loggerFactory);
            else if (Current == null)
                ConfigureCurrentLogContext();
        }

        LogContextInstrumentationExtensions.TryConfigure(provider);
    }

    /// <summary>Sets the current context only when the asynchronous flow has none.</summary>
    /// <param name="context">The context to install.</param>
    public static void SetCurrentIfNull(ILogContext? context)
    {
        Current ??= context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>Defines a one-parameter message written through the current category logger.</summary>
    /// <typeparam name="T1">The first template-value type.</typeparam>
    /// <param name="logLevel">The message severity.</param>
    /// <param name="formatString">The structured logging template.</param>
    /// <returns>The compiled logging delegate.</returns>
    public static LogMessage<T1> Define<T1>(LogLevel logLevel, string formatString)
    {
        ArgumentNullException.ThrowIfNull(formatString);

        Action<ILogger, T1, Exception?> logAction = LoggerMessage.Define<T1>(logLevel, default, formatString);

        void Log(T1 arg1, Exception? exception)
        {
            var logContext = Current;
            if (logContext != null)
                logAction(logContext.Logger, arg1, exception);
        }

        return Log;
    }

    /// <summary>Defines a two-parameter message written through the current category logger.</summary>
    /// <typeparam name="T1">The first template-value type.</typeparam>
    /// <typeparam name="T2">The second template-value type.</typeparam>
    /// <param name="logLevel">The log level.</param>
    /// <param name="formatString">The format string.</param>
    /// <returns>The log message produced by the operation.</returns>
    public static LogMessage<T1, T2> Define<T1, T2>(LogLevel logLevel, string formatString)
    {
        ArgumentNullException.ThrowIfNull(formatString);

        Action<ILogger, T1, T2, Exception?> logAction = LoggerMessage.Define<T1, T2>(logLevel, default, formatString);

        void Log(T1 arg1, T2 arg2, Exception? exception)
        {
            var logContext = Current;
            if (logContext != null)
                logAction(logContext.Logger, arg1, arg2, exception);
        }

        return Log;
    }

    /// <summary>Defines a two-parameter message written through the dedicated message logger.</summary>
    /// <typeparam name="T1">The first template-value type.</typeparam>
    /// <typeparam name="T2">The second template-value type.</typeparam>
    /// <param name="logLevel">The log level.</param>
    /// <param name="formatString">The format string.</param>
    /// <returns>The log message produced by the operation.</returns>
    public static LogMessage<T1, T2> DefineMessage<T1, T2>(LogLevel logLevel, string formatString)
    {
        ArgumentNullException.ThrowIfNull(formatString);

        Action<ILogger, T1, T2, Exception?> logAction = LoggerMessage.Define<T1, T2>(logLevel, default, formatString);

        void Log(T1 arg1, T2 arg2, Exception? exception)
        {
            var logContext = Current;
            if (logContext != null)
                logAction(logContext.Messages.Logger, arg1, arg2, exception);
        }

        return Log;
    }

    /// <summary>Defines a three-parameter message written through the current category logger.</summary>
    /// <typeparam name="T1">The first template-value type.</typeparam>
    /// <typeparam name="T2">The second template-value type.</typeparam>
    /// <typeparam name="T3">The third template-value type.</typeparam>
    /// <param name="logLevel">The log level.</param>
    /// <param name="formatString">The format string.</param>
    /// <returns>The log message produced by the operation.</returns>
    public static LogMessage<T1, T2, T3> Define<T1, T2, T3>(LogLevel logLevel, string formatString)
    {
        ArgumentNullException.ThrowIfNull(formatString);

        Action<ILogger, T1, T2, T3, Exception?> logAction = LoggerMessage.Define<T1, T2, T3>(logLevel, default, formatString);

        void Log(T1? arg1, T2 arg2, T3? arg3, Exception? exception)
        {
            var logContext = Current;
            if (logContext != null)
                logAction(logContext.Logger, arg1!, arg2, arg3!, exception);
        }

        return Log;
    }

    /// <summary>Defines a three-parameter message written through the dedicated message logger.</summary>
    /// <typeparam name="T1">The first template-value type.</typeparam>
    /// <typeparam name="T2">The second template-value type.</typeparam>
    /// <typeparam name="T3">The third template-value type.</typeparam>
    /// <param name="logLevel">The log level.</param>
    /// <param name="formatString">The format string.</param>
    /// <returns>The log message produced by the operation.</returns>
    public static LogMessage<T1, T2, T3> DefineMessage<T1, T2, T3>(LogLevel logLevel, string formatString)
    {
        ArgumentNullException.ThrowIfNull(formatString);

        Action<ILogger, T1, T2, T3, Exception?> logAction = LoggerMessage.Define<T1, T2, T3>(logLevel, default, formatString);

        void Log(T1? arg1, T2 arg2, T3? arg3, Exception? exception)
        {
            var logContext = Current;
            if (logContext != null)
                logAction(logContext.Messages.Logger, arg1!, arg2, arg3!, exception);
        }

        return Log;
    }

    /// <summary>Defines a four-parameter message written through the current category logger.</summary>
    /// <typeparam name="T1">The first template-value type.</typeparam>
    /// <typeparam name="T2">The second template-value type.</typeparam>
    /// <typeparam name="T3">The third template-value type.</typeparam>
    /// <typeparam name="T4">The fourth template-value type.</typeparam>
    /// <param name="logLevel">The log level.</param>
    /// <param name="formatString">The format string.</param>
    /// <returns>The log message produced by the operation.</returns>
    public static LogMessage<T1, T2, T3, T4> Define<T1, T2, T3, T4>(LogLevel logLevel, string formatString)
    {
        ArgumentNullException.ThrowIfNull(formatString);

        Action<ILogger, T1, T2, T3, T4, Exception?> logAction = LoggerMessage.Define<T1, T2, T3, T4>(logLevel, default, formatString);

        void Log(T1 arg1, T2 arg2, T3 arg3, T4 arg4, Exception? exception)
        {
            var logContext = Current;
            if (logContext != null)
                logAction(logContext.Logger, arg1, arg2, arg3, arg4, exception);
        }

        return Log;
    }

    /// <summary>Defines a four-parameter message written through the dedicated message logger.</summary>
    /// <typeparam name="T1">The first template-value type.</typeparam>
    /// <typeparam name="T2">The second template-value type.</typeparam>
    /// <typeparam name="T3">The third template-value type.</typeparam>
    /// <typeparam name="T4">The fourth template-value type.</typeparam>
    /// <param name="logLevel">The log level.</param>
    /// <param name="formatString">The format string.</param>
    /// <returns>The log message produced by the operation.</returns>
    public static LogMessage<T1, T2, T3, T4> DefineMessage<T1, T2, T3, T4>(LogLevel logLevel, string formatString)
    {
        ArgumentNullException.ThrowIfNull(formatString);

        Action<ILogger, T1, T2, T3, T4, Exception?> logAction = LoggerMessage.Define<T1, T2, T3, T4>(logLevel, default, formatString);

        void Log(T1 arg1, T2 arg2, T3 arg3, T4 arg4, Exception? exception)
        {
            var logContext = Current;
            if (logContext != null)
                logAction(logContext.Messages.Logger, arg1, arg2, arg3, arg4, exception);
        }

        return Log;
    }

    /// <summary>Defines a five-parameter message written through the current category logger.</summary>
    /// <typeparam name="T1">The first template-value type.</typeparam>
    /// <typeparam name="T2">The second template-value type.</typeparam>
    /// <typeparam name="T3">The third template-value type.</typeparam>
    /// <typeparam name="T4">The fourth template-value type.</typeparam>
    /// <typeparam name="T5">The fifth template-value type.</typeparam>
    /// <param name="logLevel">The log level.</param>
    /// <param name="formatString">The format string.</param>
    /// <returns>The log message produced by the operation.</returns>
    public static LogMessage<T1, T2, T3, T4, T5> Define<T1, T2, T3, T4, T5>(LogLevel logLevel, string formatString)
    {
        ArgumentNullException.ThrowIfNull(formatString);

        Action<ILogger, T1, T2, T3, T4, T5, Exception?> logAction = LoggerMessage.Define<T1, T2, T3, T4, T5>(logLevel, default, formatString);

        void Log(T1? arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, Exception? exception)
        {
            var logContext = Current;
            if (logContext != null)
                logAction(logContext.Logger, arg1!, arg2, arg3, arg4, arg5, exception);
        }

        return Log;
    }

    /// <summary>Defines a five-parameter message written through the dedicated message logger.</summary>
    /// <typeparam name="T1">The first template-value type.</typeparam>
    /// <typeparam name="T2">The second template-value type.</typeparam>
    /// <typeparam name="T3">The third template-value type.</typeparam>
    /// <typeparam name="T4">The fourth template-value type.</typeparam>
    /// <typeparam name="T5">The fifth template-value type.</typeparam>
    /// <param name="logLevel">The log level.</param>
    /// <param name="formatString">The format string.</param>
    /// <returns>The log message produced by the operation.</returns>
    public static LogMessage<T1, T2, T3, T4, T5> DefineMessage<T1, T2, T3, T4, T5>(LogLevel logLevel, string formatString)
    {
        ArgumentNullException.ThrowIfNull(formatString);

        Action<ILogger, T1, T2, T3, T4, T5, Exception?> logAction = LoggerMessage.Define<T1, T2, T3, T4, T5>(logLevel, default, formatString);

        void Log(T1? arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, Exception? exception)
        {
            var logContext = Current;
            if (logContext != null)
                logAction(logContext.Messages.Logger, arg1!, arg2, arg3, arg4, arg5, exception);
        }

        return Log;
    }

    static ILogContext CreateDefaultLogContext()
    {
        var loggerFactory = NullLoggerFactory.Instance;

        return new BusLogContext(loggerFactory);
    }
}
