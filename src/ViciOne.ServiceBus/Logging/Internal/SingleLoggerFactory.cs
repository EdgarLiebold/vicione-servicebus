using Microsoft.Extensions.Logging;

namespace ViciOne.ServiceBus.Logging.Internal;

/// <summary>Routes every requested logging category to one caller-owned logger.</summary>
internal sealed class SingleLoggerFactory : ILoggerFactory
{
    readonly ILogger _logger;

    public SingleLoggerFactory(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ILogger CreateLogger(string categoryName)
    {
        ArgumentNullException.ThrowIfNull(categoryName);
        return _logger;
    }

    public void AddProvider(ILoggerProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        throw new NotSupportedException("A single-logger adapter cannot accept additional logging providers.");
    }

    public void Dispose()
    {
        // The caller owns the wrapped logger and the adapter owns no disposable resource.
    }
}
