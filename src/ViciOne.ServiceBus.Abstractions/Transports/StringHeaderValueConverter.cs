namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a string header value converter implementation.
/// </summary>
public class StringHeaderValueConverter :
    IHeaderValueConverter
{
    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="headerValue">The header value value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(HeaderValue headerValue, out HeaderValue result)
    {
        if (headerValue.IsStringValue(out HeaderValue<string> stringValue))
        {
            result = stringValue;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="headerValue">The header value value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert<T>(HeaderValue<T> headerValue, out HeaderValue result)
    {
        if (headerValue.IsStringValue(out HeaderValue<string> stringValue))
        {
            result = stringValue;
            return true;
        }

        result = default;
        return false;
    }
}
