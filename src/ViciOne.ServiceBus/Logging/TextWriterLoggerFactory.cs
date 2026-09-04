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

    public TextWriterLoggerFactory(TextWriter textWriter, IOptions<TextWriterLoggerOptions> options)
    {
        Writer = textWriter;
        _options = options.Value;
    }

    public TextWriter Writer { get; }

    public ILogger CreateLogger(string name)
    {
        if (_options.IsEnabled(name))
            return new TextWriterLogger(this, _options.LogLevel);

        return NullLogger.Instance;
    }

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public void Dispose()
    {
    }
}
