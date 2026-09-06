namespace ViciOne.ServiceBus.Transports;

/// <summary>Converts simple header value values.</summary>
public class SimpleHeaderValueConverter :
    IHeaderValueConverter
{
    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="headerValue">The header value to convert or store.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(HeaderValue headerValue, out HeaderValue result)
    {
        if (headerValue.IsSimpleValue(out var simpleValue))
        {
            result = simpleValue;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>Attempts to convert the supplied value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="headerValue">The header value to convert or store.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert<T>(HeaderValue<T> headerValue, out HeaderValue result)
    {
        if (headerValue.IsSimpleValue(out var simpleValue))
        {
            result = simpleValue;
            return true;
        }

        result = default;
        return false;
    }
}
