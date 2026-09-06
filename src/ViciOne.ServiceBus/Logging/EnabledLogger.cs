using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.ServiceBus.Logging;

/// <summary>Represents an enabled logger.</summary>
public readonly struct EnabledLogger
{
    readonly ILogger _logger;
    readonly LogLevel _level;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="level">The level.</param>
    public EnabledLogger(ILogger logger, LogLevel level)
    {
        _logger = logger;
        _level = level;
    }

    /// <summary>Writes the current diagnostic event.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="args">The args.</param>
    public void Log(string message, params object?[] args)
    {
        _logger.Log(_level, message, args);
    }

    /// <summary>Writes the current diagnostic event.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="args">The args.</param>
    public void Log(Exception exception, string message, params object?[] args)
    {
        _logger.Log(_level, exception, message, args);
    }
}
