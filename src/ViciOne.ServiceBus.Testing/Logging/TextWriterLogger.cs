using Microsoft.Extensions.Logging;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Formats one logging category for deterministic human-readable test output.</summary>
internal sealed class TextWriterLogger : ILogger
{
    readonly string _categoryName;
    readonly TextWriterLoggerFactory _factory;
    readonly LogLevel _minimumLevel;
    readonly TimeProvider _timeProvider;

    public TextWriterLogger(
        TextWriterLoggerFactory factory,
        string categoryName,
        LogLevel minimumLevel,
        TimeProvider? timeProvider = null)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);
        if (!Enum.IsDefined(minimumLevel))
            throw new ArgumentOutOfRangeException(nameof(minimumLevel), minimumLevel, "The minimum level must be a defined LogLevel value.");
        _categoryName = categoryName;
        _minimumLevel = minimumLevel;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull => EmptyScope.Instance;

    public bool IsEnabled(LogLevel logLevel) =>
        logLevel != LogLevel.None && logLevel >= _minimumLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        if (!IsEnabled(logLevel))
            return;

        string message = formatter(state, exception);
        if (string.IsNullOrEmpty(message))
            return;

        string entry = $"{_timeProvider.GetLocalNow():HH:mm:ss.fff} {LevelCode(logLevel)} [{_categoryName}] {message}";
        if (exception is not null)
            entry += Environment.NewLine + Environment.NewLine + exception;

        _factory.WriteLine(entry);
    }

    static string LevelCode(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRC",
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        LogLevel.Critical => "CRT",
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "The log level cannot be formatted."),
    };

    sealed class EmptyScope : IDisposable
    {
        public static EmptyScope Instance { get; } = new();

        public void Dispose()
        {
            // The scope carries no state and owns no resource.
        }
    }
}
