using Microsoft.Extensions.Logging;

namespace ViciOne.ServiceBus.Logging;

/// <summary>Used to provide access to logging and diagnostic services.</summary>
public interface ILogContext
{
    /// <summary>Gets the logger.</summary>
    ILogger Logger { get; }

    /// <summary>The log context for all message movement, sent, received, etc.</summary>
    ILogContext Messages { get; }

    /// <summary>Gets the critical.</summary>
    EnabledLogger? Critical { get; }
    /// <summary>Gets the debug.</summary>
    EnabledLogger? Debug { get; }
    /// <summary>Gets the error.</summary>
    EnabledLogger? Error { get; }
    /// <summary>Gets the info.</summary>
    EnabledLogger? Info { get; }
    /// <summary>Gets the trace.</summary>
    EnabledLogger? Trace { get; }
    /// <summary>Gets the warning.</summary>
    EnabledLogger? Warning { get; }

    /// <summary>Creates a new <see cref="T:Microsoft.Extensions.Logging.ILogger" /> instance.</summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.Logging.ILogger" />.</returns>
    ILogContext CreateLogContext(string categoryName);
}
