using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Creates category-aware loggers that serialize test output to one text stream.</summary>
internal sealed class TextWriterLoggerFactory : ILoggerFactory
{
    readonly TextWriterLoggerOptions _options;
    readonly object _writeLock = new();
    readonly TextWriter _writer;
    readonly TimeProvider _timeProvider;

    public TextWriterLoggerFactory(TextWriter writer, TextWriterLoggerOptions options, TimeProvider? timeProvider = null)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (!Enum.IsDefined(options.MinimumLevel))
            throw new ArgumentOutOfRangeException(nameof(options), options.MinimumLevel, "MinimumLevel must be a defined LogLevel value.");
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public ILogger CreateLogger(string categoryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);
        return _options.IsCategoryEnabled(categoryName)
            ? new TextWriterLogger(this, categoryName, _options.MinimumLevel, _timeProvider)
            : NullLogger.Instance;
    }

    public void AddProvider(ILoggerProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        throw new NotSupportedException("The text-writer test logger is a complete logger factory and does not accept external providers.");
    }

    public void Dispose()
    {
        // The caller owns the supplied writer and the factory owns no disposable resource.
    }

    internal void WriteLine(string message)
    {
        lock (_writeLock)
            _writer.WriteLine(message);
    }
}
