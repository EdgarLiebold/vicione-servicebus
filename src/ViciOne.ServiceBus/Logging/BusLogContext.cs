using Microsoft.Extensions.Logging;

namespace ViciOne.ServiceBus.Logging;

/// <summary>Carries state for bus log operations.</summary>
public class BusLogContext :
    ILogContext
{
    readonly ILoggerFactory _loggerFactory;
    readonly ILogContext _messageLogger;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="loggerFactory">The logger factory.</param>
    public BusLogContext(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        Logger = loggerFactory.CreateLogger(LogCategoryName.ViciOneServiceBus);

        _messageLogger = new BusLogContext(loggerFactory, loggerFactory.CreateLogger("ViciOne.ServiceBus.Messages"));
    }

    BusLogContext(ILoggerFactory loggerFactory, ILogContext messageLogger, ILogger logger)
    {
        _loggerFactory = loggerFactory;
        _messageLogger = messageLogger;
        Logger = logger;
    }

    BusLogContext(ILoggerFactory loggerFactory, ILogger logger)
    {
        _loggerFactory = loggerFactory;
        Logger = logger;

        _messageLogger = this;
    }

    ILogContext ILogContext.Messages => _messageLogger;

    /// <summary>Creates log context.</summary>
    /// <param name="categoryName">The category name.</param>
    /// <returns>The created log context.</returns>
    public ILogContext CreateLogContext(string categoryName)
    {
        var logger = _loggerFactory.CreateLogger(categoryName);

        return new BusLogContext(_loggerFactory, _messageLogger, logger);
    }

    /// <summary>Gets the logger.</summary>
    public ILogger Logger { get; }

    /// <summary>Gets the critical.</summary>
    public EnabledLogger? Critical => Logger.IsEnabled(LogLevel.Critical) ? new EnabledLogger(Logger, LogLevel.Critical) : default(EnabledLogger?);

    /// <summary>Gets the debug.</summary>
    public EnabledLogger? Debug => Logger.IsEnabled(LogLevel.Debug) ? new EnabledLogger(Logger, LogLevel.Debug) : default(EnabledLogger?);

    /// <summary>Gets the error.</summary>
    public EnabledLogger? Error => Logger.IsEnabled(LogLevel.Error) ? new EnabledLogger(Logger, LogLevel.Error) : default(EnabledLogger?);

    /// <summary>Gets the info.</summary>
    public EnabledLogger? Info => Logger.IsEnabled(LogLevel.Information) ? new EnabledLogger(Logger, LogLevel.Information) : default(EnabledLogger?);

    /// <summary>Gets the trace.</summary>
    public EnabledLogger? Trace => Logger.IsEnabled(LogLevel.Trace) ? new EnabledLogger(Logger, LogLevel.Trace) : default(EnabledLogger?);

    /// <summary>Gets the warning.</summary>
    public EnabledLogger? Warning => Logger.IsEnabled(LogLevel.Warning) ? new EnabledLogger(Logger, LogLevel.Warning) : default(EnabledLogger?);
}
