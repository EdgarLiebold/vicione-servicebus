using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace ViciOne.ServiceBus.Logging;

/// <summary>
/// Defines configuration options for text writer logger.
/// </summary>
public sealed class TextWriterLoggerOptions
{
    readonly List<string> _disabled;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public TextWriterLoggerOptions()
    {
        _disabled = new List<string>();
    }

    /// <summary>
    /// Gets or sets the log level value.
    /// </summary>
    public LogLevel LogLevel { get; set; }

    /// <summary>
    /// Performs the disable operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the operation.</returns>
    public TextWriterLoggerOptions Disable(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _disabled.Add(name);

        return this;
    }

    /// <summary>
    /// Determines whether enabled.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsEnabled(string name)
    {
        return !_disabled.Any(x => name.StartsWith(x, StringComparison.OrdinalIgnoreCase));
    }
}
