namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides mutable transport metadata for an outgoing message.</summary>
public interface SendHeaders :
    Headers
{
    /// <summary>Sets a text header, or removes it when the value is <see langword="null" />.</summary>
    /// <param name="key">The header name.</param>
    /// <param name="value">The text value, or <see langword="null" /> to remove the header.</param>
    void Set(string key, string? value);

    /// <summary>Sets a header value, optionally preserving an existing value.</summary>
    /// <param name="key">The header name.</param>
    /// <param name="value">The value, or <see langword="null" /> to remove the header when overwriting.</param>
    /// <param name="overwrite"><see langword="true" /> to replace or remove an existing header; <see langword="false" /> to add only when absent.</param>
    void Set(string key, object? value, bool overwrite = true);
}
