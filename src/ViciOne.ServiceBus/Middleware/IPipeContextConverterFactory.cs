namespace ViciOne.ServiceBus.Middleware;

/// <summary>Creates pipe context converter instances.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
public interface IPipeContextConverterFactory<in TInput>
    where TInput : class, PipeContext
{
    /// <summary>
    /// Given a known input context type, convert it to the correct output
    /// context type.
    /// </summary>
    /// <typeparam name="TOutput">The output type.</typeparam>
    /// <returns>The converter.</returns>
    IPipeContextConverter<TInput, TOutput> GetConverter<TOutput>()
        where TOutput : class, PipeContext;
}
