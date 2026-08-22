using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace ViciOne.ServiceBus.SignalR.Tests;

internal sealed class RecordingLogger : ILogger
{
    private readonly Channel<LogEntry> _publishedEntries = Channel.CreateUnbounded<LogEntry>(
        new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = false,
            SingleWriter = false,
        });

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull =>
        null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public async Task<LogEntry> WaitForAsync(
        Func<LogEntry, bool> predicate,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        await foreach (var entry in _publishedEntries.Reader.ReadAllAsync(timeoutSource.Token))
        {
            if (predicate(entry))
            {
                return entry;
            }
        }

        throw new EndOfStreamException("The SignalR log stream completed before the expected entry was written.");
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        var entry = new LogEntry(logLevel, formatter(state, exception), exception);
        _publishedEntries.Writer.TryWrite(entry);
    }
}

internal sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);
