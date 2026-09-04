using Microsoft.Extensions.Logging;

#nullable enable
namespace ViciOne.ServiceBus.Logging;

/// <summary>
/// Used to provide access to logging and diagnostic services
/// </summary>
public interface ILogContext
{
    /// <summary>
    /// Gets the logger value.
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// The log context for all message movement, sent, received, etc.
    /// </summary>
    ILogContext Messages { get; }

    /// <summary>
    /// Gets the critical value.
    /// </summary>
    EnabledLogger? Critical { get; }
    /// <summary>
    /// Gets the debug value.
    /// </summary>
    EnabledLogger? Debug { get; }
    /// <summary>
    /// Gets the error value.
    /// </summary>
    EnabledLogger? Error { get; }
    /// <summary>
    /// Gets the info value.
    /// </summary>
    EnabledLogger? Info { get; }
    /// <summary>
    /// Gets the trace value.
    /// </summary>
    EnabledLogger? Trace { get; }
    /// <summary>
    /// Gets the warning value.
    /// </summary>
    EnabledLogger? Warning { get; }

    /// <summary>
    /// Creates a new <see cref="T:Microsoft.Extensions.Logging.ILogger" /> instance.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.Logging.ILogger" />.</returns>
    ILogContext CreateLogContext(string categoryName);
}
