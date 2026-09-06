namespace ViciOne.ServiceBus.Middleware;

/// <summary>Converts the input context to the output context.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TOutput">The output type.</typeparam>
public interface IPipeContextConverter<in TInput, TOutput>
    where TInput : class, PipeContext
    where TOutput : class, PipeContext
{
    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="output">Receives the output produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryConvert(TInput input, [NotNullWhen(true)] out TOutput? output);
}
