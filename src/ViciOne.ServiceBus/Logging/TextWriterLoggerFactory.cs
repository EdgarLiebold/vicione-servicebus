using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ViciOne.ServiceBus.Logging;

/// <summary>Creates text writer logger instances.</summary>
public class TextWriterLoggerFactory :
    ILoggerFactory
{
    readonly TextWriterLoggerOptions _options;
    readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="textWriter">The text writer.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public TextWriterLoggerFactory(TextWriter textWriter, IOptions<TextWriterLoggerOptions> options, TimeProvider? timeProvider = null)
    {
        Writer = textWriter;
        _options = options.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Gets the writer.</summary>
    public TextWriter Writer { get; }

    /// <summary>Creates logger.</summary>
    /// <param name="name">The name.</param>
    /// <returns>The created logger.</returns>
    public ILogger CreateLogger(string name)
    {
        if (_options.IsEnabled(name))
            return new TextWriterLogger(this, _options.LogLevel, _timeProvider);

        return NullLogger.Instance;
    }

    /// <summary>Adds provider to the configuration.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    public void AddProvider(ILoggerProvider provider)
    {
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
    }
}
