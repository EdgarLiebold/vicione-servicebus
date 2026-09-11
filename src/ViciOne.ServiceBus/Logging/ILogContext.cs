using Microsoft.Extensions.Logging;

namespace ViciOne.ServiceBus.Logging;

/// <summary>Provides category-specific Microsoft logging for the active service-bus operation.</summary>
public interface ILogContext
{
    /// <summary>Gets the logger for this context's category.</summary>
    ILogger Logger { get; }

    /// <summary>Gets the dedicated context for message-movement logs.</summary>
    ILogContext Messages { get; }

    /// <summary>Gets a critical-level writer when that level is enabled.</summary>
    EnabledLogger? Critical { get; }
    /// <summary>Gets a debug-level writer when that level is enabled.</summary>
    EnabledLogger? Debug { get; }
    /// <summary>Gets an error-level writer when that level is enabled.</summary>
    EnabledLogger? Error { get; }
    /// <summary>Gets an information-level writer when that level is enabled.</summary>
    EnabledLogger? Info { get; }
    /// <summary>Gets a trace-level writer when that level is enabled.</summary>
    EnabledLogger? Trace { get; }
    /// <summary>Gets a warning-level writer when that level is enabled.</summary>
    EnabledLogger? Warning { get; }

    /// <summary>Creates a context for another logging category.</summary>
    /// <param name="categoryName">The non-empty category name.</param>
    /// <returns>A context that writes to the requested category.</returns>
    ILogContext CreateLogContext(string categoryName);
}
