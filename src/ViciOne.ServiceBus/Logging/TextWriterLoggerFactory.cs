using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

#nullable enable
namespace ViciOne.ServiceBus.Logging;

/// <summary>
/// Provides a text writer logger factory implementation.
/// </summary>
public class TextWriterLoggerFactory :
    ILoggerFactory
{
    readonly TextWriterLoggerOptions _options;
    readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="textWriter">The text writer value.</param>
    /// <param name="options">The options value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public TextWriterLoggerFactory(TextWriter textWriter, IOptions<TextWriterLoggerOptions> options, TimeProvider? timeProvider = null)
    {
        Writer = textWriter;
        _options = options.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Gets the writer value.
    /// </summary>
    public TextWriter Writer { get; }

    /// <summary>
    /// Creates logger.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the operation.</returns>
    public ILogger CreateLogger(string name)
    {
        if (_options.IsEnabled(name))
            return new TextWriterLogger(this, _options.LogLevel, _timeProvider);

        return NullLogger.Instance;
    }

    /// <summary>
    /// Adds provider to the configuration.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    public void AddProvider(ILoggerProvider provider)
    {
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    public void Dispose()
    {
    }
}
