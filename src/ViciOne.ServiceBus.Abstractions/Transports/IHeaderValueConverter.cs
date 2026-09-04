namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for header value converter.
/// </summary>
public interface IHeaderValueConverter
{
    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="headerValue">The header value value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryConvert(HeaderValue headerValue, out HeaderValue result);
    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="headerValue">The header value value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryConvert<T>(HeaderValue<T> headerValue, out HeaderValue result);
}


/// <summary>
/// Defines the contract for header value converter.
/// </summary>
/// <typeparam name="TValueType">The t value type type.</typeparam>
public interface IHeaderValueConverter<TValueType>
{
    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="headerValue">The header value value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryConvert(HeaderValue headerValue, out HeaderValue<TValueType> result);
    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="headerValue">The header value value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryConvert<T>(HeaderValue<T> headerValue, out HeaderValue<TValueType> result);
}
