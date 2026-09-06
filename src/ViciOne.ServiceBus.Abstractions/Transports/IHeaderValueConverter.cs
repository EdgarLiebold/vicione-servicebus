namespace ViciOne.ServiceBus.Transports;

/// <summary>Defines the operations required by header value converter.</summary>
public interface IHeaderValueConverter
{
    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="headerValue">The header value to convert or store.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryConvert(HeaderValue headerValue, out HeaderValue result);
    /// <summary>Attempts to convert the supplied value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="headerValue">The header value to convert or store.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryConvert<T>(HeaderValue<T> headerValue, out HeaderValue result);
}


/// <summary>Defines the operations required by header value converter.</summary>
/// <typeparam name="TValueType">The value type type.</typeparam>
public interface IHeaderValueConverter<TValueType>
{
    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="headerValue">The header value to convert or store.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryConvert(HeaderValue headerValue, out HeaderValue<TValueType> result);
    /// <summary>Attempts to convert the supplied value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="headerValue">The header value to convert or store.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryConvert<T>(HeaderValue<T> headerValue, out HeaderValue<TValueType> result);
}
