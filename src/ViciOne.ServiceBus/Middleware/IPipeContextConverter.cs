// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Middleware
{
    /// <summary>
    /// Converts the input context to the output context
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TOutput"></typeparam>
    public interface IPipeContextConverter<in TInput, TOutput>
        where TInput : class, PipeContext
        where TOutput : class, PipeContext
    {
        bool TryConvert(TInput input, out TOutput output);
    }
}
