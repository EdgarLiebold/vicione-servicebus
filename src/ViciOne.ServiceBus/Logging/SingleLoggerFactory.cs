using Microsoft.Extensions.Logging;

namespace ViciOne.ServiceBus.Logging;

/// <summary>Creates single logger instances.</summary>
public class SingleLoggerFactory :
    ILoggerFactory
{
    readonly ILogger _logger;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="logger">The logger.</param>
    public SingleLoggerFactory(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>Creates logger.</summary>
    /// <param name="name">The name.</param>
    /// <returns>The created logger.</returns>
    public ILogger CreateLogger(string name)
    {
        return _logger;
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
