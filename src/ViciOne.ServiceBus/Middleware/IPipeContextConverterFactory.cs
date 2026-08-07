// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Middleware
{
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
}
