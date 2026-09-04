using System;
using Microsoft.Extensions.Logging;

#nullable enable
namespace ViciOne.ServiceBus.Logging;

/// <summary>
/// Represents an enabled logger value.
/// </summary>
public readonly struct EnabledLogger
{
    readonly ILogger _logger;
    readonly LogLevel _level;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="logger">The logger value.</param>
    /// <param name="level">The level value.</param>
    public EnabledLogger(ILogger logger, LogLevel level)
    {
        _logger = logger;
        _level = level;
    }

    /// <summary>
    /// Performs the log operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="args">The args value.</param>
    public void Log(string message, params object?[] args)
    {
        _logger.Log(_level, message, args);
    }

    /// <summary>
    /// Performs the log operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="message">The message value.</param>
    /// <param name="args">The args value.</param>
    public void Log(Exception exception, string message, params object?[] args)
    {
        _logger.Log(_level, exception, message, args);
    }
}
