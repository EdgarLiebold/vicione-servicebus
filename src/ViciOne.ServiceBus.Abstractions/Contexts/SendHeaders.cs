namespace ViciOne.ServiceBus.Advanced;

/// <summary>Defines the operations required by send headers.</summary>
public interface SendHeaders :
    Headers
{
    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The value to process.</param>
    void Set(string key, string? value);
    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="overwrite">The overwrite.</param>
    void Set(string key, object? value, bool overwrite = true);
}
