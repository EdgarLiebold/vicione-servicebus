namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for send headers.
/// </summary>
public interface SendHeaders :
    Headers
{
    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    void Set(string key, string? value);
    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    /// <param name="overwrite">The overwrite value.</param>
    void Set(string key, object? value, bool overwrite = true);
}
