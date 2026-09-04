using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Provides a log context implementation.
/// </summary>
public static class LogContext
{
    static readonly AsyncLocal<ILogContext?> _current;

    static LogContext()
    {
        _current = new AsyncLocal<ILogContext?>();
    }

    /// <summary>
    /// Gets the critical value.
    /// </summary>
    public static EnabledLogger? Critical => Current?.Critical;
    /// <summary>
    /// Gets the debug value.
    /// </summary>
    public static EnabledLogger? Debug => Current?.Debug;
    /// <summary>
    /// Gets the error value.
    /// </summary>
    public static EnabledLogger? Error => Current?.Error;
    /// <summary>
    /// Gets the info value.
    /// </summary>
    public static EnabledLogger? Info => Current?.Info;
    /// <summary>
    /// Gets the trace value.
    /// </summary>
    public static EnabledLogger? Trace => Current?.Trace;
    /// <summary>
    /// Gets the warning value.
    /// </summary>
    public static EnabledLogger? Warning => Current?.Warning;

    /// <summary>
    /// Gets or sets the current operation (Activity) for the current thread.  This flows
    /// across async calls.
    /// </summary>
    public static ILogContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    /// <summary>
    /// Configures current log context.
    /// </summary>
    /// <param name="loggerFactory">The logger factory value.</param>
    public static void ConfigureCurrentLogContext(ILoggerFactory? loggerFactory = null)
    {
        Current = new BusLogContext(loggerFactory ?? NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Configure the current <see cref="LogContext" /> using the specified <paramref name="logger" />, which will be
    /// used for all log output.
    /// </summary>
    /// <param name="logger">An existing logger</param>
    public static void ConfigureCurrentLogContext(ILogger logger)
    {
        Current = new BusLogContext(new SingleLoggerFactory(logger));
    }

    /// <summary>
    /// Creates log context.
    /// </summary>
    /// <param name="categoryName">The category name value.</param>
    /// <returns>The result of the operation.</returns>
    public static ILogContext CreateLogContext(string categoryName)
    {
        var current = Current ??= CreateDefaultLogContext();
        var created = current.CreateLogContext(categoryName);

        LogContextInstrumentationExtensions.CopyInstrumentation(current, created);
        return created;
    }

    /// <summary>
    /// If <see cref="Current"/> is not null or the null logger, configure the current LogContext
    /// using the specified service provider.
    /// </summary>
    /// <param name="provider"></param>
    public static void ConfigureCurrentLogContextIfNull(IServiceProvider provider)
    {
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

    /// <summary>
    /// Sets current if null.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public static void SetCurrentIfNull(ILogContext? context)
    {
        Current ??= context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Performs the define operation.
    /// </summary>
    /// <typeparam name="T1">The t1 type.</typeparam>
    /// <param name="logLevel">The log level value.</param>
    /// <param name="formatString">The format string value.</param>
    /// <returns>The result of the operation.</returns>
    public static LogMessage<T1> Define<T1>(LogLevel logLevel, string formatString)
    {
        Action<ILogger, T1, Exception?> logAction = LoggerMessage.Define<T1>(logLevel, default, formatString);

        void Log(T1 arg1, Exception? exception)
        {
            var logContext = Current;
            if (logContext != null)
                logAction(logContext.Logger, arg1, exception);
        }

        return Log;
    }

    /// <summary>
    /// Performs the define operation.
    /// </summary>
    /// <typeparam name="T1">The t1 type.</typeparam>
    /// <typeparam name="T2">The t2 type.</typeparam>
    /// <param name="logLevel">The log level value.</param>
    /// <param name="formatString">The format string value.</param>
    /// <returns>The result of the operation.</returns>
    public static LogMessage<T1, T2> Define<T1, T2>(LogLevel logLevel, string formatString)
    {
        Action<ILogger, T1, T2, Exception?> logAction = LoggerMessage.Define<T1, T2>(logLevel, default, formatString);

        void Log(T1 arg1, T2 arg2, Exception? exception)
        {
            var logContext = Current;
            if (logContext != null)
                logAction(logContext.Logger, arg1, arg2, exception);
        }

        return Log;
    }

    /// <summary>
    /// Performs the define message operation.
    /// </summary>
    /// <typeparam name="T1">The t1 type.</typeparam>
    /// <typeparam name="T2">The t2 type.</typeparam>
    /// <param name="logLevel">The log level value.</param>
    /// <param name="formatString">The format string value.</param>
    /// <returns>The result of the operation.</returns>
    public static LogMessage<T1, T2> DefineMessage<T1, T2>(LogLevel logLevel, string formatString)
    {
        Action<ILogger, T1, T2, Exception?> logAction = LoggerMessage.Define<T1, T2>(logLevel, default, formatString);

        void Log(T1 arg1, T2 arg2, Exception? exception)
        {
            var logContext = Current;
            if (logContext != null)
                logAction(logContext.Messages.Logger, arg1, arg2, exception);
        }

        return Log;
    }

    /// <summary>
    /// Performs the define operation.
    /// </summary>
    /// <typeparam name="T1">The t1 type.</typeparam>
    /// <typeparam name="T2">The t2 type.</typeparam>
    /// <typeparam name="T3">The t3 type.</typeparam>
    /// <param name="logLevel">The log level value.</param>
    /// <param name="formatString">The format string value.</param>
    /// <returns>The result of the operation.</returns>
    public static LogMessage<T1, T2, T3> Define<T1, T2, T3>(LogLevel logLevel, string formatString)
    {
        Action<ILogger, T1, T2, T3, Exception?> logAction = LoggerMessage.Define<T1, T2, T3>(logLevel, default, formatString);

        void Log(T1? arg1, T2 arg2, T3? arg3, Exception? exception)
        {
            var logContext = Current;
            if (logContext != null)
                logAction(logContext.Logger, arg1!, arg2, arg3!, exception);
        }

        return Log;
    }

    /// <summary>
    /// Performs the define message operation.
    /// </summary>
    /// <typeparam name="T1">The t1 type.</typeparam>
    /// <typeparam name="T2">The t2 type.</typeparam>
    /// <typeparam name="T3">The t3 type.</typeparam>
    /// <param name="logLevel">The log level value.</param>
    /// <param name="formatString">The format string value.</param>
    /// <returns>The result of the operation.</returns>
    public static LogMessage<T1, T2, T3> DefineMessage<T1, T2, T3>(LogLevel logLevel, string formatString)
    {
        Action<ILogger, T1, T2, T3, Exception?> logAction = LoggerMessage.Define<T1, T2, T3>(logLevel, default, formatString);

        void Log(T1? arg1, T2 arg2, T3? arg3, Exception? exception)
        {
            var logContext = Current;
            if (logContext != null)
                logAction(logContext.Messages.Logger, arg1!, arg2, arg3!, exception);
        }

        return Log;
    }

    /// <summary>
    /// Performs the define operation.
    /// </summary>
    /// <typeparam name="T1">The t1 type.</typeparam>
    /// <typeparam name="T2">The t2 type.</typeparam>
    /// <typeparam name="T3">The t3 type.</typeparam>
    /// <typeparam name="T4">The t4 type.</typeparam>
    /// <param name="logLevel">The log level value.</param>
    /// <param name="formatString">The format string value.</param>
    /// <returns>The result of the operation.</returns>
    public static LogMessage<T1, T2, T3, T4> Define<T1, T2, T3, T4>(LogLevel logLevel, string formatString)
    {
        Action<ILogger, T1, T2, T3, T4, Exception?> logAction = LoggerMessage.Define<T1, T2, T3, T4>(logLevel, default, formatString);

        void Log(T1 arg1, T2 arg2, T3 arg3, T4 arg4, Exception? exception)
        {
            var logContext = Current;
            if (logContext != null)
                logAction(logContext.Logger, arg1, arg2, arg3, arg4, exception);
        }

        return Log;
    }

    /// <summary>
    /// Performs the define message operation.
    /// </summary>
    /// <typeparam name="T1">The t1 type.</typeparam>
    /// <typeparam name="T2">The t2 type.</typeparam>
    /// <typeparam name="T3">The t3 type.</typeparam>
    /// <typeparam name="T4">The t4 type.</typeparam>
    /// <param name="logLevel">The log level value.</param>
    /// <param name="formatString">The format string value.</param>
    /// <returns>The result of the operation.</returns>
    public static LogMessage<T1, T2, T3, T4> DefineMessage<T1, T2, T3, T4>(LogLevel logLevel, string formatString)
    {
        Action<ILogger, T1, T2, T3, T4, Exception?> logAction = LoggerMessage.Define<T1, T2, T3, T4>(logLevel, default, formatString);

        void Log(T1 arg1, T2 arg2, T3 arg3, T4 arg4, Exception? exception)
        {
            var logContext = Current;
            if (logContext != null)
                logAction(logContext.Messages.Logger, arg1, arg2, arg3, arg4, exception);
        }

        return Log;
    }

    /// <summary>
    /// Performs the define operation.
    /// </summary>
    /// <typeparam name="T1">The t1 type.</typeparam>
    /// <typeparam name="T2">The t2 type.</typeparam>
    /// <typeparam name="T3">The t3 type.</typeparam>
    /// <typeparam name="T4">The t4 type.</typeparam>
    /// <typeparam name="T5">The t5 type.</typeparam>
    /// <param name="logLevel">The log level value.</param>
    /// <param name="formatString">The format string value.</param>
    /// <returns>The result of the operation.</returns>
    public static LogMessage<T1, T2, T3, T4, T5> Define<T1, T2, T3, T4, T5>(LogLevel logLevel, string formatString)
    {
        Action<ILogger, T1, T2, T3, T4, T5, Exception?> logAction = LoggerMessage.Define<T1, T2, T3, T4, T5>(logLevel, default, formatString);

        void Log(T1? arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, Exception? exception)
        {
            var logContext = Current;
            if (logContext != null)
                logAction(logContext.Logger, arg1!, arg2, arg3, arg4, arg5, exception);
        }

        return Log;
    }

    /// <summary>
    /// Performs the define message operation.
    /// </summary>
    /// <typeparam name="T1">The t1 type.</typeparam>
    /// <typeparam name="T2">The t2 type.</typeparam>
    /// <typeparam name="T3">The t3 type.</typeparam>
    /// <typeparam name="T4">The t4 type.</typeparam>
    /// <typeparam name="T5">The t5 type.</typeparam>
    /// <param name="logLevel">The log level value.</param>
    /// <param name="formatString">The format string value.</param>
    /// <returns>The result of the operation.</returns>
    public static LogMessage<T1, T2, T3, T4, T5> DefineMessage<T1, T2, T3, T4, T5>(LogLevel logLevel, string formatString)
    {
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
