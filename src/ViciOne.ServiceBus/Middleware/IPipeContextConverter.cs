namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Converts the input context to the output context
/// </summary>
/// <typeparam name="TInput"></typeparam>
/// <typeparam name="TOutput"></typeparam>
public interface IPipeContextConverter<in TInput, TOutput>
    where TInput : class, PipeContext
    where TOutput : class, PipeContext
{
    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="output">The output value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryConvert(TInput input, [NotNullWhen(true)] out TOutput? output);
}
