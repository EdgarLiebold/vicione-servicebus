using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Provides extension methods for respond.
/// </summary>
public static class RespondExtensions
{
    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static EventActivityBinder<TInstance, TData> Respond<TInstance, TData, TMessage>(this EventActivityBinder<TInstance, TData> source,
        TMessage message, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new RespondActivity<TInstance, TData, TMessage>(MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static EventActivityBinder<TInstance, TData> RespondAsync<TInstance, TData, TMessage>(this EventActivityBinder<TInstance, TData> source,
        Task<TMessage> message, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new RespondActivity<TInstance, TData, TMessage>(MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static EventActivityBinder<TInstance, TData> Respond<TInstance, TData, TMessage>(this EventActivityBinder<TInstance, TData> source,
        EventMessageFactory<TInstance, TData, TMessage> messageFactory,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new RespondActivity<TInstance, TData, TMessage>(MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static EventActivityBinder<TInstance, TData> RespondAsync<TInstance, TData, TMessage>(this EventActivityBinder<TInstance, TData> source,
        AsyncEventMessageFactory<TInstance, TData, TMessage> messageFactory,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new RespondActivity<TInstance, TData, TMessage>(MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static EventActivityBinder<TInstance, TData> RespondAsync<TInstance, TData, TMessage>(this EventActivityBinder<TInstance, TData> source,
        Func<BehaviorContext<TInstance, TData>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new RespondActivity<TInstance, TData, TMessage>(MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TException> Respond<TInstance, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TException> source, TMessage message,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedRespondActivity<TInstance, TException, TMessage>(MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TException> RespondAsync<TInstance, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TException> source, Task<TMessage> message,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedRespondActivity<TInstance, TException, TMessage>(MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TException> Respond<TInstance, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TException> source,
        EventExceptionMessageFactory<TInstance, TException, TMessage> messageFactory,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedRespondActivity<TInstance, TException, TMessage>(MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TException> RespondAsync<TInstance, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TException> source,
        AsyncEventExceptionMessageFactory<TInstance, TException, TMessage> messageFactory,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedRespondActivity<TInstance, TException, TMessage>(MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TData, TException> Respond<TInstance, TData, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TData, TException> source, TMessage message,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedRespondActivity<TInstance, TData, TException, TMessage>(MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TData, TException> RespondAsync<TInstance, TData, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TData, TException> source, Task<TMessage> message,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedRespondActivity<TInstance, TData, TException, TMessage>(MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TData, TException> Respond<TInstance, TData, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TData, TException> source,
        EventExceptionMessageFactory<TInstance, TData, TException, TMessage> messageFactory,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedRespondActivity<TInstance, TData, TException, TMessage>(MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>
    /// Performs the respond operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TData, TException> RespondAsync<TInstance, TData, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TData, TException> source,
        AsyncEventExceptionMessageFactory<TInstance, TData, TException, TMessage> messageFactory,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedRespondActivity<TInstance, TData, TException, TMessage>(MessageFactory<TMessage>.Create(messageFactory, callback)));
    }
}
