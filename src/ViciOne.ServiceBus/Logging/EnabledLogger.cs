using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.ServiceBus.Logging;

/// <summary>Writes messages at one level that the current logger has enabled.</summary>
public sealed class EnabledLogger
{
    readonly ILogger _logger;
    readonly LogLevel _level;

    internal EnabledLogger(ILogger logger, LogLevel level)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _level = level;
    }

    /// <summary>Writes a structured message at the enabled level.</summary>
    /// <param name="message">The structured logging template.</param>
    /// <param name="args">The values bound to template placeholders.</param>
    public void Log(string message, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(message);
        _logger.Log(_level, message, args);
    }

    /// <summary>Writes a structured message and its associated exception at the enabled level.</summary>
    /// <param name="exception">The exception associated with the message.</param>
    /// <param name="message">The structured logging template.</param>
    /// <param name="args">The values bound to template placeholders.</param>
    public void Log(Exception exception, string message, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(message);
        _logger.Log(_level, exception, message, args);
    }
}
