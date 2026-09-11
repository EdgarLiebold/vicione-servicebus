using Microsoft.Extensions.Logging;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Configures the logger used to write service-bus test output to a text stream.</summary>
public sealed class TextWriterLoggerOptions
{
    readonly HashSet<string> _suppressedCategories = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets the least severe level written to the stream.</summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

    /// <summary>Suppresses a logging category and all of its descendants.</summary>
    /// <param name="categoryName">The category whose logging hierarchy should be suppressed.</param>
    /// <returns>This options instance.</returns>
    public TextWriterLoggerOptions SuppressCategory(string categoryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);
        _suppressedCategories.Add(categoryName.Trim());
        return this;
    }

    internal bool IsCategoryEnabled(string categoryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);
        return !_suppressedCategories.Any(prefix =>
            categoryName.Equals(prefix, StringComparison.OrdinalIgnoreCase)
            || (categoryName.Length > prefix.Length
                && categoryName[prefix.Length] == '.'
                && categoryName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
    }
}
