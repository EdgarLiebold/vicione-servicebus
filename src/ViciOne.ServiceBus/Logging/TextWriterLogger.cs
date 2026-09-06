using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.ServiceBus.Logging;

/// <summary>Writes text writer log entries.</summary>
public class TextWriterLogger :
    ILogger
{
    readonly TextWriterLoggerFactory _factory;
    readonly LogLevel _logLevel;
    readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="logLevel">The log level.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public TextWriterLogger(TextWriterLoggerFactory factory, LogLevel logLevel, TimeProvider? timeProvider = null)
    {
        _factory = factory;
        _logLevel = logLevel;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Creates a scope for the current operation.</summary>
    /// <typeparam name="TState">The state carried by the operation.</typeparam>
    /// <param name="state">The state.</param>
    /// <returns>The disposable produced by the operation.</returns>
    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return TestDisposable.Instance;
    }

    /// <summary>Writes the current diagnostic event.</summary>
    /// <typeparam name="TState">The state carried by the operation.</typeparam>
    /// <param name="logLevel">The log level.</param>
    /// <param name="eventId">The event id.</param>
    /// <param name="state">The state.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="formatter">The formatter.</param>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        if (formatter == null)
            throw new ArgumentNullException(nameof(formatter));

        var message = formatter(state, exception);

        if (string.IsNullOrEmpty(message))
            return;

        message = $"{_timeProvider.GetLocalNow():HH:mm:ss.fff}-{logLevel.ToString()[0]} {message}";

        if (exception != null)
            message += Environment.NewLine + Environment.NewLine + exception;

        _factory.Writer.WriteLine(message);
    }

    /// <summary>Determines whether enabled.</summary>
    /// <param name="logLevel">The log level.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _logLevel;
    }


    class TestDisposable : IDisposable
    {
        public static readonly TestDisposable Instance = new TestDisposable();

        public void Dispose()
        {
            // The shared sentinel owns no disposable resource.
        }
    }
}
