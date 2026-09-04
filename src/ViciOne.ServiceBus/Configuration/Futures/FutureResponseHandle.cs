namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for future response handle.
/// </summary>
/// <typeparam name="TCommand">The t command type.</typeparam>
/// <typeparam name="TResult">The t result type.</typeparam>
/// <typeparam name="TFault">The t fault type.</typeparam>
/// <typeparam name="TRequest">The t request type.</typeparam>
/// <typeparam name="TResponse">The t response type.</typeparam>
public interface FutureResponseHandle<out TCommand, TResult, TFault, TRequest, out TResponse> :
    FutureRequestHandle<TCommand, TResult, TFault, TRequest>
    where TCommand : class
    where TResult : class
    where TFault : class
    where TRequest : class
    where TResponse : class
{
    /// <summary>
    /// The Response Completed event
    /// </summary>
    Event<TResponse> Completed { get; }
}
