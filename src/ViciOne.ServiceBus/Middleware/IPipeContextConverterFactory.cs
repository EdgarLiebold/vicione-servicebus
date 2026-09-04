namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Defines the contract for pipe context converter factory.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
public interface IPipeContextConverterFactory<in TInput>
    where TInput : class, PipeContext
{
    /// <summary>
    /// Given a known input context type, convert it to the correct output
    /// context type.
    /// </summary>
    /// <typeparam name="TOutput"></typeparam>
    /// <returns></returns>
    IPipeContextConverter<TInput, TOutput> GetConverter<TOutput>()
        where TOutput : class, PipeContext;
}
