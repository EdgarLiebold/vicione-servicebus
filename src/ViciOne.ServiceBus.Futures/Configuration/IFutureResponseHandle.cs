namespace ViciOne.ServiceBus.Configuration;

/// <summary>Exposes the completion event for a response accepted by a future request.</summary>
/// <typeparam name="TCommand">The command that created the future.</typeparam>
/// <typeparam name="TResult">The successful future result contract.</typeparam>
/// <typeparam name="TFault">The future fault contract.</typeparam>
/// <typeparam name="TRequest">The request contract.</typeparam>
/// <typeparam name="TResponse">The accepted response contract.</typeparam>
public interface IFutureResponseHandle<out TCommand, TResult, TFault, TRequest, out TResponse> :
    IFutureRequestHandle<TCommand, TResult, TFault, TRequest>
    where TCommand : class
    where TResult : class
    where TFault : class
    where TRequest : class
    where TResponse : class
{
    /// <summary>Gets the event raised when the response is received.</summary>
    Event<TResponse> Completed { get; }
}
