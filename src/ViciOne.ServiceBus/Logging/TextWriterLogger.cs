using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.ServiceBus.Logging;

/// <summary>
/// Provides a text writer logger implementation.
/// </summary>
public class TextWriterLogger :
    ILogger
{
    readonly TextWriterLoggerFactory _factory;
    readonly LogLevel _logLevel;
    readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <param name="logLevel">The log level value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public TextWriterLogger(TextWriterLoggerFactory factory, LogLevel logLevel, TimeProvider? timeProvider = null)
    {
        _factory = factory;
        _logLevel = logLevel;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Performs the begin scope operation.
    /// </summary>
    /// <typeparam name="TState">The t state type.</typeparam>
    /// <param name="state">The state value.</param>
    /// <returns>The result of the operation.</returns>
    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return TestDisposable.Instance;
    }

    /// <summary>
    /// Performs the log operation.
    /// </summary>
    /// <typeparam name="TState">The t state type.</typeparam>
    /// <param name="logLevel">The log level value.</param>
    /// <param name="eventId">The event id value.</param>
    /// <param name="state">The state value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="formatter">The formatter value.</param>
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

    /// <summary>
    /// Determines whether enabled.
    /// </summary>
    /// <param name="logLevel">The log level value.</param>
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
            // intentionally does nothing
        }
    }
}
