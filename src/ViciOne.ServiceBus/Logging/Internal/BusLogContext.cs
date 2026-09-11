using Microsoft.Extensions.Logging;

namespace ViciOne.ServiceBus.Logging.Internal;

/// <summary>Adapts an <see cref="ILoggerFactory"/> to the service-bus logging context.</summary>
internal sealed class BusLogContext : ILogContext
{
    readonly ILoggerFactory _loggerFactory;
    readonly ILogContext _messageLogger;

    public BusLogContext(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        Logger = loggerFactory.CreateLogger(ServiceBusLogCategories.Root);
        _messageLogger = new BusLogContext(loggerFactory, loggerFactory.CreateLogger(ServiceBusLogCategories.Messages));
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

    public ILogger Logger { get; }

    public ILogContext Messages => _messageLogger;

    public EnabledLogger? Critical => CreateEnabledLogger(LogLevel.Critical);

    public EnabledLogger? Debug => CreateEnabledLogger(LogLevel.Debug);

    public EnabledLogger? Error => CreateEnabledLogger(LogLevel.Error);

    public EnabledLogger? Info => CreateEnabledLogger(LogLevel.Information);

    public EnabledLogger? Trace => CreateEnabledLogger(LogLevel.Trace);

    public EnabledLogger? Warning => CreateEnabledLogger(LogLevel.Warning);

    public ILogContext CreateLogContext(string categoryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);
        ILogger logger = _loggerFactory.CreateLogger(categoryName);
        return new BusLogContext(_loggerFactory, _messageLogger, logger);
    }

    EnabledLogger? CreateEnabledLogger(LogLevel level) =>
        Logger.IsEnabled(level) ? new EnabledLogger(Logger, level) : null;
}
