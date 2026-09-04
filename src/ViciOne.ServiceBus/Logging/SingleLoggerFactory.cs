using Microsoft.Extensions.Logging;

#nullable enable
namespace ViciOne.ServiceBus.Logging;

/// <summary>
/// Provides a single logger factory implementation.
/// </summary>
public class SingleLoggerFactory :
    ILoggerFactory
{
    readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="logger">The logger value.</param>
    public SingleLoggerFactory(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates logger.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the operation.</returns>
    public ILogger CreateLogger(string name)
    {
        return _logger;
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
