namespace ViciOne.ServiceBus.Configuration;

/// <summary>Controls the lifetime of future response.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
/// <typeparam name="TFault">The fault type.</typeparam>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface FutureResponseHandle<out TCommand, TResult, TFault, TRequest, out TResponse> :
    FutureRequestHandle<TCommand, TResult, TFault, TRequest>
    where TCommand : class
    where TResult : class
    where TFault : class
    where TRequest : class
    where TResponse : class
{
    /// <summary>The Response Completed event.</summary>
    Event<TResponse> Completed { get; }
}
