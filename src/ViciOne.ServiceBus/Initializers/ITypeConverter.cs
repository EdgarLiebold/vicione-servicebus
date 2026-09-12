namespace ViciOne.ServiceBus.Initializers;

/// <summary>Attempts a synchronous conversion without throwing for an unsupported value.</summary>
/// <typeparam name="TResult">The converted value type.</typeparam>
/// <typeparam name="TInput">The source value type.</typeparam>
public interface ITypeConverter<TResult, in TInput>
{
    /// <summary>Attempts to convert <paramref name="input"/> to <typeparamref name="TResult"/>.</summary>
    /// <param name="input">The source value.</param>
    /// <param name="result">Receives the converted value when conversion succeeds.</param>
    /// <returns><see langword="true"/> when the value is representable as <typeparamref name="TResult"/>.</returns>
    bool TryConvert(TInput? input, out TResult? result);
}
