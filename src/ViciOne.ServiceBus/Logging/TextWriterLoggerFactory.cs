using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

#nullable enable
namespace ViciOne.ServiceBus.Logging;

public class TextWriterLoggerFactory :
    ILoggerFactory
{
    readonly TextWriterLoggerOptions _options;
    readonly TimeProvider _timeProvider;

    public TextWriterLoggerFactory(TextWriter textWriter, IOptions<TextWriterLoggerOptions> options, TimeProvider? timeProvider = null)
    {
        Writer = textWriter;
        _options = options.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public TextWriter Writer { get; }

    public ILogger CreateLogger(string name)
    {
        if (_options.IsEnabled(name))
            return new TextWriterLogger(this, _options.LogLevel, _timeProvider);

        return NullLogger.Instance;
    }

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public void Dispose()
    {
    }
}
